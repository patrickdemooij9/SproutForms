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

### Descriptors (Umbraco Backoffice)
Located in `SproutForms.Umbraco.Core/Descriptors/`:
- **Fields**: `TextFieldDescriptor`, `EmailFieldDescriptor`, `TextAreaFieldDescriptor`, `SelectFieldDescriptor`, `RadioFieldDescriptor`, `DateFieldDescriptor`, `FileFieldDescriptor`, `HiddenFieldDescriptor`
- **Workflows**: `EmailWorkflowDescriptor`
- **Outcomes**: `RedirectUrlOutcomeDescriptor`, `ShowMessageOutcomeDescriptor`, `RedirectUmbracoPageOutcomeDescriptor`

### Services
- `IFormSubmissionService` / `FormSubmissionService` - Handles form submissions
- `FormRenderingService` - Renders forms to HTML

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

## Adding New Field Types

1. Create descriptor in `SproutForms.Umbraco.Core/Descriptors/Fields/`
2. Create config model in `SproutForms.Core/Flows/Configs/`
3. Create view in `SproutForms.Umbraco.Core/Views/Forms/Fields/`
4. Register in `SproutFormComposer.cs`

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
