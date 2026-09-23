---
name: verify-in-site
description: Run SproutForms in a throwaway Umbraco site and verify a change end-to-end - render a form, submit it in the browser pane, then check the stored submission, the workflow executions and the captured emails. Use after changing submission handling, validation, field types, rendering, forms.ts, workflows or repositories, or when asked to test, verify or try out a change in the real site.
---

# Verify a change in the running site

The `AiTest` environment of `src/SproutForms.Site` is a disposable test rig:

- **Database:** a throwaway SQLite file at `src/SproutForms.Site/umbraco/Data/AiTest/`, installed unattended on first start. The admin password is random per start, so the backoffice is out of scope; never try to sign in.
- **Forms:** code-first forms from `src/SproutForms.Site/Code/`, registered on startup in this environment only.
  - `testFormCode` and `testFileForm` are the demo forms.
  - `aiTestRequiredCheckbox` is a regression form: one required and one optional checkbox.
- **Email:** goes to the pickup folder `src/SproutForms.Site/umbraco/Data/AiTest/Mail/*.eml`. Nothing is ever delivered, whatever address a form's workflow names.
- **Test endpoints** (`src/SproutForms.Site/Controllers/AiTestController.cs`), which return 404 outside `AiTest`:
  - `GET /ai-test/forms` lists the forms, with alias and source.
  - `GET /ai-test/forms/{alias}` renders one form on a bare page, with the real `forms.js` and CSS.
  - `GET /ai-test/forms/{alias}/submissions?take=10` returns the newest submissions, their stored values and every workflow execution (status, attempts, last error).

## The loop

1. **Rebuild what you changed.**
   - C#: the site's `dotnet run` builds, so nothing extra is needed. Restart the server after C# changes (`preview_stop`, then step 2).
   - `forms.ts`: the site serves the committed bundle `wwwroot/forms.js`, so rebuild it from `src/SproutForms.Umbraco/assets` with `npm run minify:forms`. Without that, your TypeScript change isn't what runs.
2. **Start the site:** `preview_start` with name `sproutforms-aitest` (`.claude/launch.json`, port 62970). The first start does the unattended install. Confirm in `preview_logs` (search `Application started`) that the SproutForms migrations and `WorkflowExecutionWorker` started.
3. **Open the form:** navigate to `http://localhost:62970/ai-test/forms/testFormCode`, or to a form you added to `Code/` and registered in `Program.cs`.
4. **Exercise it:**
   - Get refs from `read_page` with `filter: interactive`, fill fields with `form_input`, and click radios and Submit **by `ref`**. Coordinate clicks taken from a scaled screenshot miss.
   - Read the result with `get_page_text` or `javascript_tool`. Errors render as `.form-error` next to the field and `.form-global-errors` at the top. Success replaces the form with the outcome message.
   - Always cover **both** an invalid submit (missing required fields, bad input) and a valid one. Also cover whatever case the change is about.
5. **Check what was stored:** wait about 10 seconds (the workflow worker runs every 10s), then `fetch('/ai-test/forms/{alias}/submissions')` from `javascript_tool`.
   - The submission should have the expected `values` and a `pageUrl`.
   - Every workflow should reach `Succeeded`. For a failure, read `lastError` and `preview_logs`.
6. **Check the emails:** read the newest `.eml` in the Mail folder. Look at the headers, and check that submitted values are HTML-encoded in the body.
7. **Report:** say what you exercised, the observed results (quote the errors, statuses and email lines), and anything that failed. A step you couldn't run counts as unverified; say so rather than claiming it passed.
8. **Stop the site** with `preview_stop` when you're done.

## Resetting

For a clean install (a schema or migration change, or leftover data getting in the way), stop the site, then run:

```bash
pwsh -NoProfile -File scripts/ai-test/reset.ps1
```

It deletes `umbraco/Data/AiTest/`, which holds the database and the mail. Uploaded files are not reset: `LocalDiskFileStorageProvider` writes to the shared `App_Data/SproutForms/Uploads`.

## Gotchas

- **Check where an error came from.** Client-side validation can show the same message the server would. `read_network_requests` with `urlPattern: "/api/forms/"` shows whether the submit reached the server, and with which status (400 = rejected, 200 = accepted).
- **Resubmit without reloading.** After a failed submit, fix the input and submit again on the same page. That's what visitors do, and it catches client-side state corrupted by the error handling.
- **Refs go stale.** If the pane was closed and reopened, `ref`s from an earlier `read_page` or `find` no longer work, so look them up again. When `read_page` returns an empty page, `find` still works.
- **Nothing to watch in reCAPTCHA.** It is off: `NoFormSubmissionGuard` is registered, so the guard always allows.
- **Ignore the restore warnings.** The `NU1902`/`NU1903` package warnings flood the start of `preview_logs`, so search the logs rather than reading the head.
