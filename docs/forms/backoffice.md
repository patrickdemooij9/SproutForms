# Forms in the backoffice

With Umbraco, editors build forms in the SproutForms section of the backoffice. Users need the section on one of their user groups; the Administrators group gets it when SproutForms is installed. See [Install with Umbraco](../getting-started/umbraco.md).

## The forms list

The section's tree holds the forms, in folders if you like. Create a form or a folder from the tree's actions. When more than one [form type](../extending/form-types.md) is registered for the backoffice, Create asks which kind of form it is; that can't be changed afterwards.

The Overview dashboard shows how the forms are doing: submissions over the last 30 days, recent submissions, and the workflows that ran or failed. It also holds the workflow templates (see [Workflows](#workflows)).

Forms defined in code show in the list too, with their submissions and history, but they can't be edited, rolled back or deleted in the backoffice. Change them in code; see [Code only forms](code-first.md).

## Editing a form

A form has four tabs:

- **Build**: the form's pages, rows and fields. Add fields to the canvas, and click a field to edit its label, whether it's required, its settings, and its rules on its Rules tab (see [Field rules](field-rules.md)). The form's pages show above the canvas, see [Pages](pages.md).
- **Settings**: the submit button's text, whether the progress steps of a paged form show, the outcome (what the visitor sees after a submit), conditional outcomes and calculations (see [Calculations](calculations.md)), and the form type's own settings.
- **Integrations**: the workflows that run after a submission is saved.
- **Info**: the form's history.

Saving checks the form: every field is placed on exactly one page, only a form's single page may be empty, and a condition only uses fields from earlier pages (or, for a field, its own page). A form type adds its own checks. Saving fails with a message when one of them doesn't hold.

The fields and workflows you can add are the [built-in ones](built-in-types.md), plus any custom type that has a backoffice descriptor (see [Extending SproutForms](../extending/README.md)).

## Rendering it on a page

Add a property with the "SproutForms form picker" editor to a document type, or render a form by its alias in a view. See [Install with Umbraco](../getting-started/umbraco.md#rendering-a-form).

## Workflows

Add workflows on the Integrations tab. They run in the background after a submission is saved, in the order they're listed: each waits until the ones before it have succeeded.

Workflow templates, on the Overview dashboard, hold the settings many forms share, such as the address an email goes to. A form's workflow can start from a template. The settings the template locks always come from the template, so changing the template changes every form that uses it.

## Submissions

Every form's submissions are stored and can be viewed in the backoffice, opened from the forms list. A submission shows its answers, the page it was sent from, and the [variables](calculations.md) the form worked out. It also shows each workflow's status: pending, running, succeeded, failed or retrying. Retry a failed workflow from there.

Only what the visitor could see is stored: the values of fields hidden by their rules or on a skipped page are dropped, along with their uploads.

The visitor's IP address is only stored when `SproutForms:StoreIpAddress` is on, see [Configuration](../configuration.md).

## History and rollback

The Info tab lists what happened to the form: created, saved, renamed, moved, rolled back, moved to the recycle bin and back, and submissions deleted or restored. Every save is a new version. Compare a version with the current one, and roll back to it, from there.

## Recycle bins

Deleting a form moves it to the recycle bin. A form in the recycle bin is treated as deleted everywhere but the bin: it doesn't render, takes no submissions, and its workflows pause. Restore it, or delete it for good, from the bin.

Submissions have a recycle bin per form, on its submissions page. Deleting submissions moves them there, to be restored or deleted for good.

What has been in a recycle bin longer than `SproutForms:RecycleBin:RetentionDays` (default 30) is deleted for good, by a job that runs every hour. `0` keeps it until someone deletes it. Submissions hold personal data, so they shouldn't stay in the bin forever.
