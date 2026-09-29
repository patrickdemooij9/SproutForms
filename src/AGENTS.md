# SproutForms Development Guide

## Project Overview

SproutForms is an Umbraco CMS plugin that enables creating and managing forms in code or through the backoffice.

### Projects

| Project | Description | Dependencies |
|---------|-------------|--------------|
| `SproutForms.Core` | Core library for creating forms in code | Microsoft.AspNetCore.Mvc |
| `SproutForms.Umbraco.Core` | Umbraco plugin core with backoffice, repositories, descriptors | Umbraco.Cms.Web.Website, Umbraco.Cms.Api.Management |
| `SproutForms.Umbraco` | Umbraco plugin package | SproutForms.Umbraco.Core |
| `SproutForms.Site` | Demo/test site | - |
| `SproutForms.Client` | `@sproutforms/client`, the npm package for the headless API, with an example app in `example/` | - |

### Target Framework
- .NET 10.0

## Building

Build the backend with:
```bash
dotnet build src/SproutForms.sln
```

Build the frontend with the following command inside of the src/SproutForms.Umbraco folder
```bash
npm run build
```

## Key Architecture

### Domain Models (`SproutForms.Core/Models/`)
- **Form**, **FormField**, **FormPage**, **FormRow**, **FormColumn** - Core form structure. A definition's layout is a list of pages, each holding rows of columns that point to a field by alias
- **FormSubmission** - Captured form entries
- **Flows**: `FormWorkflow`, `WorkflowExecution`, `WorkflowExecutionStatus`
- **Outcomes**: `ShowMessageOutcome`, `RedirectUrlOutcome`, `RedirectUmbracoPageOutcome`
- **Submission Guards**: `IFormSubmissionGuard`, `HoneypotSubmissionGuard` (the default), `RecaptchaV3SubmissionGuard`, `NoFormSubmissionGuard`. Only one guard is active: registering another `IFormSubmissionGuard` replaces the honeypot. A guard that needs markup in the form sets `PartialViewPath`.

### Repositories
- `IFormRepository` / `FormRepository`
- `IFormSubmissionRepository` / `FormSubmissionRepository`
- `IFormVersionRepository` / `FormVersionRepository`
- `IFolderRepository` / `FolderRepository`
- `IWorkflowExecutionRepository` / `WorkflowExecutionRepository`
- `IFormAuditRepository` / `FormAuditRepository` - a form's history (created, saved, renamed, rolled back)

### Descriptors (Umbraco Backoffice)
Located in `SproutForms.Umbraco.Core/Descriptors/`:
- **Fields**: `TextFieldDescriptor`, `EmailFieldDescriptor`, `TextAreaFieldDescriptor`, `SelectFieldDescriptor`, `RadioFieldDescriptor`, `DateFieldDescriptor`, `FileFieldDescriptor`, `HiddenFieldDescriptor`
- **Workflows**: `EmailWorkflowDescriptor`
- **Outcomes**: `RedirectUrlOutcomeDescriptor`, `ShowMessageOutcomeDescriptor`, `RedirectUmbracoPageOutcomeDescriptor`

### Services
- `IFormSubmissionService` / `FormSubmissionService` - Handles form submissions
- `FormClientModelBuilder` - Builds a published form's `FormClientModel` (`SproutForms.Core/Models/ClientModels/`): what a front-end may see of it. Its pages hold rows of columns, each with its field, the same shape as the Razor view model. The headless API returns it as-is, and `FormRenderingService` builds the Razor view model from it, so both show the same form. Field types filter their configuration with `IFormFieldType.GetClientConfiguration`, form types their settings and field extensions with `GetClientSettings` / `GetClientFieldExtension` (nothing by default)
- `FormRenderingService` - The Razor view model: the client model with the whole field configuration and the values and errors of a post without JavaScript
- `FormSubmitOutcomeRunner` - Runs a form's submit outcome for a saved submission, for both submission controllers
- `FormThemeViewResolver` - Finds the view to render: `~/Views/Forms/Themes/{theme}/{view}.cshtml` when the theme has it, otherwise `~/Views/Forms/{view}.cshtml`. The theme is passed to the partials in `ViewData`, so the package's views render each other with `Html.SproutFormsPartialAsync("Rows", model)`, never with a hard-coded path
- `FormRecycleBinService` - Deleting a form in the backoffice moves it to the recycle bin (`Form.TrashedAt`). A trashed form is treated as deleted everywhere but the bin: it doesn't render, takes no submissions, and its workflows pause. `FormDeletionService` deletes it for good from the bin, and `RecycleBinCleanupJob` does so after `SproutForms:RecycleBin:RetentionDays` (default 30, 0 keeps it)
- `FormSubmissionRecycleBinService` - The same for submissions (`FormSubmission.TrashedAt`), with a recycle bin per form in its Submissions tab. `IFormSubmissionRepository.GetByForm` and `Count` leave trashed submissions out, `GetAllByForm` includes them. Each action is one entry in the form's history, however many submissions it covered

### Database Entities
Located in `SproutForms.Umbraco.Core/Models/Database/`:
- `FormEntity`, `FormVersionEntity`, `FormSubmissionEntity`, `FolderEntity`, `WorkflowExecutionEntity`

### ViewModels
- Backoffice models in `SproutForms.Umbraco.Core/Models/ViewModels/`
- Render models in `SproutForms.Core/Models/ViewModels/`

### Database Migrations
Located in `SproutForms.Umbraco.Core/Startup/Migrations/`:
- `FormsInitialMigration` - Initial schema
- `AddFoldersMigration` - Folder support
- `FormsUserGroupMigration` - User group setup
- `AddSubmissionResultsMigration` - `ResultsJson` column for the results a form type computes
- `WrapRowsInPagesMigration` - moves the rows of every stored definition into a single page
- `AddFormAuditMigration` - `SproutForms_FormAudit` table for a form's history
- `AddFormRecycleBinMigration` - `TrashedAt` and `TrashedBy` columns that put a form in the recycle bin
- `AddSubmissionRecycleBinMigration` - the same columns for submissions

## Headless API

`SproutFormsDeliveryController` (`/umbraco/sproutforms/delivery/api/v1`, OpenAPI document `sproutforms-delivery`) serves `FormClientModel`s and takes submissions as JSON or multipart, without antiforgery. It is off unless `SproutForms:Headless:Enabled`; `HeadlessApiAccessAttribute` answers 404 then, and checks `SproutForms:Headless:ApiKey`. `AddSproutFormsHeadless` (`Headless/`) registers its CORS policy (`SproutForms:Headless:AllowedOrigins`), the CORS middleware and the OpenAPI document, only when enabled at startup. The Razor `FormSubmissionController` (`/api/forms`) stays as it is; both call `IFormSubmissionService` and `FormSubmitOutcomeRunner`.

`src/SproutForms.Client` holds the conditions and validation rules in TypeScript. `forms.ts` imports them, so `npm run minify:forms` bundles them into `forms.js`; keep them in step with `ConditionEvaluator` and the field types' `Validate`. After changing the API's models, regenerate `src/api/types.gen.ts` with `npm run generate` in `src/SproutForms.Client` while the `AiTest` site runs.

## Adding New Field Types

1. Create descriptor in `SproutForms.Umbraco.Core/Descriptors/Fields/`
2. Create config model in `SproutForms.Core/Flows/Configs/`
3. Create view in `SproutForms.Umbraco.Core/Views/Forms/Fields/`
4. Override `GetClientConfiguration` when the config holds settings the browser mustn't see, and add its config to `BuiltInFieldConfigurations` in `src/SproutForms.Client/src/types.ts`
5. Register in `SproutFormComposer.cs`

## Form Definition Types

A form type (`IFormDefinitionType`, in `SproutForms.Core/Models/FormTypes/`) says what kind of form a definition is, such as standard, quiz or poll. The README's "Extending: form types" section is the user guide, and `SproutForms.Site/Examples/` has a working quiz, poll and product finder. It is stored per version in `FormDefinition.Type`, together with the type's typed settings. Definitions stored before form types existed read as `standard`. A form's type is chosen when it is created and can't be changed afterwards.

- Build a type on `FormDefinitionTypeBase<TSettings>`, and override `AllowsFieldType` / `AllowsOutcomeType` to exclude types.
- A type can add settings to every field of a field type with `ExtendField<TFieldSettings>(fieldTypeAlias)` in its constructor, such as the correct answer of a quiz question. A field stores them in `FormField.Extension`.
- A type can process a submission by overriding `ProcessSubmissionAsync`. It runs after field validation, before the submission is saved, and returns **results** to store with it (such as a quiz score, in `FormSubmission.Results`) or errors that reject it. `GetSettings` and `GetFieldSettings<T>` give the form's and a field's typed settings.
- A submit outcome (`IFormSubmitOutcomeType.HandleAsync`) gets a `FormSubmitOutcomeContext` with its configuration, the saved submission (values and results) and the form version. An outcome that throws is logged, and the visitor gets the default confirmation, because the submission is already saved. forms.js shows a `message` as HTML, so never put submitted values in it.
- A field or outcome type that only belongs in certain form types implements `IRestrictedToFormTypes`.
- `FormDefinitionStructureValidator` checks a definition's layout (every field on exactly one page, no empty page unless it's the only one, conditions only use fields from earlier pages) at the same two moments as the type validator below.
- `FormDefinitionTypeValidator` enforces these rules. It runs when a code-first form is registered (an invalid form fails startup) and when the backoffice saves a form.
- Code-first forms choose a type with `FormBuilder.OfType(alias, settings)`, set a field's extension settings with `FieldBuilder.Extend(settings)`, use any outcome type with `FormBuilder.SetOutcome(alias, configuration)`, and any workflow type with `WorkflowBuilder.Add(alias, workflowTypeAlias, configuration)`.
- Register a type as `IFormDefinitionType` in a composer.
- For the backoffice, also register an `IFormDefinitionTypeDescriptor`: build it on `BaseFormDefinitionTypeDescriptor<TSettings>`, map the form settings with `DefineMap`, and describe each field extension with `ExtendField<TFieldSettings>(fieldTypeAlias, field => field.Map(...))`. A type without a descriptor can be used by code-first forms, but editors can't choose it.
- A field property can use the `SproutForms.FieldOptionPicker` editor, a dropdown of the edited field's own options (for field types that keep them under `options`, like radio buttons and dropdowns). Custom field editors are `formFieldConfig` extensions, and receive the field being edited as `formField`.
- In the backoffice, Create asks for the form type when more than one type has a descriptor. The Settings tab shows the type and its settings, the Build tab only offers the field types the type allows, and an extended field gets a tab named after the form type.

## Testing

There is no automated test project yet. To verify a change end-to-end in a running site (throwaway SQLite database, emails captured to disk), follow the `verify-in-site` skill in `.claude/skills/verify-in-site/SKILL.md`. It uses the `AiTest` launch profile of `SproutForms.Site`.
