# Custom workflow types for headless

Workflows only run on the server, after a submission is saved, whether the form was submitted from a Razor page or through the headless API. A headless front-end needs nothing for them, and the definition it gets never holds a form's workflows or their settings.

Build and register the workflow type as in [Custom workflow types](../extending/workflow-types.md). A submission through the headless API queues its workflows the same way, and the backoffice shows how they went.

What a workflow sees of the submission:

- `Submission.Values`: what the front-end sent for the form's fields. Values of fields hidden by their rules, or on a skipped page, are dropped before the submission is saved.
- `Submission.PageUrl`: the `sf_PageUrl` the client sends (the current page in a browser, or `pageUrl` in `client.submit`), only when it's on the Umbraco site or one of `SproutForms:Headless:AllowedOrigins`.
- `Submission.Variables`: always worked out again on the server, whatever the front-end sent.

To show the visitor something that a workflow did, such as a ticket number from a CRM, use an [outcome type](../extending/outcome-types.md) instead: outcomes run while the visitor waits, workflows don't.
