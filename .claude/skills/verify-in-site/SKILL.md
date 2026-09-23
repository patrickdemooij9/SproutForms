---
name: verify-in-site
description: Run SproutForms in a throwaway Umbraco site and verify a change end-to-end - render a form, submit it in the browser pane, then check the stored submission, the workflow executions and the captured emails. Use after changing submission handling, validation, field types, rendering, forms.ts, workflows or repositories, or when asked to test, verify or try out a change in the real site.
---

# Verify a change in the running site

The `AiTest` environment of `src/SproutForms.Site` is a disposable test rig:

- **Database:** a throwaway SQLite file at `src/SproutForms.Site/umbraco/Data/AiTest/`, installed unattended on first start. The admin password is random per start, so the backoffice is out of scope; never try to sign in.
- **Forms:** code-first forms from `src/SproutForms.Site/Code/`, registered on startup in this environment only.
  - `testFormCode` and `testFileForm` are the demo forms.
  - Regression forms, one per awkward case:
    - `aiTestRequiredCheckbox`: one required and one optional checkbox.
    - `aiTestEdgeCases`: a regex containing `"`, a regex that backtracks badly (`^(a+)+$`), and an upload field whose storage provider doesn't exist.
    - `aiTestFailingWorkflow`: a Custom POST workflow to a dead port, so it always fails. Use it for retry and failure handling.
    - `aiTestWorkflowOrder`: three workflows (email, dead-port Custom POST, email). The first should reach `Succeeded`, the second `Failed`, and the third stay `Pending` behind it. The stored `Order` values are 0, 1, 2.
    - `aiTestUnknownOutcome`: its submit outcome type isn't registered. A submit is still saved, and `forms.js` shows its fallback confirmation.
- **Email:** goes to the pickup folder `src/SproutForms.Site/umbraco/Data/AiTest/Mail/*.eml`. Nothing is ever delivered, whatever address a form's workflow names.
- **Uploads:** stored in `src/SproutForms.Site/umbraco/Data/AiTest/Uploads/`, set by `SproutForms:LocalDiskFileStorage:RootPath` in `appsettings.AiTest.json`.
- **Settings:** `appsettings.AiTest.json` also sets `SproutForms:StoreIpAddress` (`false`, the package default). Edits to it apply without a restart. Put it back when you're done.
- **Test endpoints** (`src/SproutForms.Site/Controllers/AiTestController.cs`), which return 404 outside `AiTest`:
  - `GET /ai-test/forms` lists the forms, with alias and source.
  - `GET /ai-test/forms/{alias}` renders one form on a bare page, with the real `forms.js` and CSS.
  - `GET /ai-test/forms/{alias}/submissions?take=10` returns the newest submissions, their stored values, the IP address and every workflow execution (status, attempts, last error).
  - `POST /ai-test/submissions/{submissionId}/workflows/{workflowAlias}/retry` and `DELETE /ai-test/forms/{alias}` call the same services as the backoffice's retry and delete, which need a signed-in user. Deleting a code-first form only lasts until the next start, when it is registered again.

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

It deletes `umbraco/Data/AiTest/`, which holds the database, the mail and the uploads.

## Going below the UI

Some cases can't be reached through the rendered form. Client-side validation stops the request, the browser pane can't pick a file, or you need a response header.

- **Post directly from the page** with `javascript_tool`. Build a `FormData` holding the page's `__RequestVerificationToken`, `sf_PageUrl` and the fields, add a file as `new Blob([...])` with a file name, and `fetch(form.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body })`. The JSON response carries `errors`. Time the call with `performance.now()` when the change is about speed.
- **Check a non-AJAX submit (the redirect)** from PowerShell, since `fetch` can't read a redirect's `Location`. Use `Invoke-WebRequest` with a `WebRequestSession`: GET the page for the token and cookie, then POST with `-MaximumRedirection 0 -SkipHttpErrorCheck` and read `$r.Headers.Location`. PowerShell prints a "maximum redirection count" error each time; the response is still returned.
- **Query the database** with Python's `sqlite3` module (`python` is on the PATH). Open `umbraco/Data/AiTest/SproutForms.sqlite.db` read-only (`file:...?mode=ro`, `uri=True`). The tables are `SproutForms_Forms`, `SproutForms_FormVersions`, `SproutForms_FormSubmissions` and `SproutForms_WorkflowExecutions`, with GUIDs stored as text, so compare with `lower(...)`. Use it to count rows before and after a delete.

## Gotchas

- **Check where an error came from.** Client-side validation can show the same message the server would. `read_network_requests` with `urlPattern: "/api/forms/"` shows whether the submit reached the server, and with which status (400 = rejected, 200 = accepted).
- **Resubmit without reloading.** After a failed submit, fix the input and submit again on the same page. That's what visitors do, and it catches client-side state corrupted by the error handling.
- **Refs go stale.** If the pane was closed and reopened, `ref`s from an earlier `read_page` or `find` no longer work, so look them up again. When `read_page` returns an empty page, `find` still works.
- **Nothing to watch in reCAPTCHA.** It is off: `NoFormSubmissionGuard` is registered, so the guard always allows.
- **Ignore the restore warnings.** The `NU1902`/`NU1903` package warnings flood the start of `preview_logs`, so search the logs rather than reading the head.
