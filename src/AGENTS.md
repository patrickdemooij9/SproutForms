# SproutForms Development Guide

## Project Overview

SproutForms is an Umbraco CMS plugin that enables creating and managing forms in code or through the backoffice.

### Projects

| Project | Description | Dependencies |
|---------|-------------|--------------|
| `SproutForms.Core` | Razor class library with everything that doesn't need Umbraco: forms in code, rendering (views, `forms.js`, CSS), submissions, workflows, in-memory storage | Microsoft.AspNetCore.App |
| `SproutForms.Umbraco.Core` | Umbraco plugin core with backoffice, repositories, descriptors | Umbraco.Cms.Web.Website, Umbraco.Cms.Api.Management |
| `SproutForms.Umbraco` | Umbraco plugin package | SproutForms.Umbraco.Core |
| `SproutForms.Site` | Demo/test site | - |
| `SproutForms.Standalone.Site` | ASP.NET Core MVC site without Umbraco, using `SproutForms.Core` alone | SproutForms.Core |
| `SproutForms.Client` | `@sproutforms/client`, the npm package for the headless API and its form engine, with an example app in `example/` | - |
| `SproutForms.Vue` | `@sproutforms/vue`, renders headless forms in Vue on the client's form engine, with a playground in `playground/` | `@sproutforms/client` |
| `SproutForms.Nuxt` | `@sproutforms/nuxt`, the Nuxt module around `@sproutforms/vue`: SSR and an API proxy, with a playground in `playground/` | `@sproutforms/vue` |

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

The headless packages (`SproutForms.Client`, its example, `SproutForms.Vue` and `SproutForms.Nuxt`) are an npm workspace with its root in the repository root: `npm install` there, then `npm run build` builds the client, Vue and Nuxt packages in order and `npm run typecheck` checks them. `src/SproutForms.Umbraco/assets` is not part of it.

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

### Caching
`FormRepository` and `FormVersionRepository` cache in Umbraco's runtime cache, under keys starting with `sproutForms_` (`Caching/DefaultRepositoryCachePolicy`). Every write to them, or to `WorkflowTemplateRepository` (form versions hold their templates' values), calls `DistributedCache.RefreshSproutForms()`, which runs `SproutFormsCacheRefresher` on every server, so a load balanced site doesn't serve an old form from its other servers. A new repository that caches, or whose changes show in cached forms, does the same.

### Backoffice authorization
Located in `SproutForms.Umbraco.Core/Security/`. A backoffice login alone isn't enough: the backoffice and recycle bin controllers require `SproutFormsAuthorization.SectionAccessPolicy`, the `sproutForms` section. The tree controller requires `TreeAccessPolicy` (the SproutForms, Content, Media or Members section), because the form picker browses it outside the SproutForms section; its `item` endpoint gives the picker a form's name. Anything in the tree controller that changes data, like creating a folder, adds `SectionAccessPolicy` on top. A new backoffice endpoint goes on a controller with `SectionAccessPolicy`, never `AuthorizationPolicies.BackOfficeAccess`.

### Descriptors (Umbraco Backoffice)
Located in `SproutForms.Umbraco.Core/Descriptors/`:
- **Fields**: `TextFieldDescriptor`, `EmailFieldDescriptor`, `TextAreaFieldDescriptor`, `CheckboxFieldDescriptor`, `SelectFieldDescriptor`, `RadioFieldDescriptor`, `DateFieldDescriptor`, `FileFieldDescriptor`, `HiddenFieldDescriptor`, `RepeaterFieldDescriptor`
- **Workflows**: `EmailWorkflowDescriptor`
- **Outcomes**: `RedirectUrlOutcomeDescriptor`, `ShowMessageOutcomeDescriptor`, `RedirectUmbracoPageOutcomeDescriptor`

### Services
- `IFormSubmissionService` / `FormSubmissionService` - Handles form submissions. Only what the visitor could see is stored: the values of fields hidden by their rules or on a skipped page aren't validated, so they're dropped once the submission is accepted, along with their uploads. A `hidden` field without `AllowOverrideFromClient` always holds its `DefaultValue`, whatever was posted, and conditions and calculations see that value
- `FormClientModelBuilder` - Builds a published form's `FormClientModel` (`SproutForms.Core/Models/ClientModels/`): what a front-end may see of it. Its pages hold rows of columns, each with its field, the same shape as the Razor view model. The headless API returns it as-is, and `FormRenderingService` builds the Razor view model from it, so both show the same form. Field types filter their configuration with `IFormFieldType.GetClientConfiguration`, form types their settings and field extensions with `GetClientSettings` / `GetClientFieldExtension` (nothing by default)
- `FormRenderingService` - The Razor view model: the client model with the whole field configuration and the values and errors of a post without JavaScript
- `FormSubmitOutcomeRunner` - Runs a form's submit outcome for a saved submission, for both submission controllers
- `FormThemeViewResolver` - Finds the view to render: `~/Views/Forms/Themes/{theme}/{view}.cshtml` when the theme has it, otherwise `~/Views/Forms/{view}.cshtml`. The theme is passed to the partials in `ViewData`, so the package's views render each other with `Html.SproutFormsPartialAsync("Rows", model)`, never with a hard-coded path
- `FormRecycleBinService` - Deleting a form in the backoffice moves it to the recycle bin (`Form.TrashedAt`). A trashed form is treated as deleted everywhere but the bin: it doesn't render, takes no submissions, and its workflows pause. `FormDeletionService` deletes it for good from the bin, and `RecycleBinCleanupJob` does so after `SproutForms:RecycleBin:RetentionDays` (default 30, 0 keeps it)
- `FormSubmissionRecycleBinService` - The same for submissions (`FormSubmission.TrashedAt`), with a recycle bin per form on its submissions page, opened from the forms list. `IFormSubmissionRepository.GetByForm` and `Count` leave trashed submissions out, `GetAllByForm` includes them. Each action is one entry in the form's history, however many submissions it covered

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
- `AddSubmissionVariablesMigration` - `VariablesJson` column for the variables a form's calculations work out

## Hosts: Umbraco and standalone

`SproutForms.Core` must not reference Umbraco. `AddSproutForms(configuration)` registers what every host shares: services, the built-in field, outcome and workflow types, rendering and the honeypot guard. A host adds storage (the `SproutForms.Core.Repositories` interfaces), an `IEmailSender`, and runs `CodeFormRegistrar.RegisterAll`, `PendingWorkflowProcessor` and `RecycleBinCleanupService`:

- **Umbraco:** `SproutFormComposer` calls `AddSproutForms`, then registers the NPoco repositories, `UmbracoEmailSender`, the backoffice pieces, `CodeFormUmbracoRegistar` (an Umbraco component, once the database is migrated) and the recurring background jobs.
- **Standalone:** `AddSproutFormsStandalone` adds `SmtpEmailSender` (`SproutForms:Smtp`) and the hosted services in `SproutForms.Core/Hosting/`; `AddSproutFormsInMemoryStorage` the repositories in `Storage/InMemory/`. Run `SproutForms.Standalone.Site` (launch config `sproutforms-standalone`, port 5180) to check a change there; its emails go to `App_Data/Emails`.

Put a new piece in Core unless it needs Umbraco, and register it in `AddSproutForms` so both hosts get it. Something Umbraco provides implicitly (`IHttpContextAccessor`, `IHttpClientFactory`) must be registered there too, or the standalone site fails to start.

## Headless API

`SproutFormsDeliveryController` (`/umbraco/sproutforms/delivery/api/v1`, OpenAPI document `sproutforms-delivery`) serves `FormClientModel`s and takes submissions as JSON or multipart, without antiforgery. A submission's body is one values object, the same values a Razor form posts: the fields, `sf_PageUrl` (`FormSubmissionRequest.PageUrlKey`) and the guard's own keys; each consumer takes the values it reads, and the submission service keeps only the form's fields. It is off unless `SproutForms:Headless:Enabled`; `HeadlessApiAccessAttribute` answers 404 then, and checks `SproutForms:Headless:ApiKey`. `AddSproutFormsHeadless` (`Headless/`) registers its CORS policy (`SproutForms:Headless:AllowedOrigins`), the CORS middleware and the OpenAPI document, only when enabled at startup. The Razor `FormSubmissionController` (`/api/forms`) stays as it is; both call `IFormSubmissionService` and `FormSubmitOutcomeRunner`.

`src/SproutForms.Client` holds the conditions and validation rules in TypeScript, and the form engine that runs them. The Razor forms use the same engine: `Form.cshtml` writes the `FormClientModel` into a `<script type="application/json" data-sf-definition>` (`RenderedFormViewModel.Definition`), and `forms.ts` creates an engine on it with a transport that posts form fields with the antiforgery token to `FormSubmissionController`; `npm run minify:forms` bundles it all into `forms.js`. Keep the TypeScript in step with `ConditionEvaluator`, `FormCalculator` and the field types' `Validate`. After changing the API's models, regenerate `src/api/types.gen.ts` with `npm run generate` in `src/SproutForms.Client` while the `AiTest` site runs.

## Front-end packages

`@sproutforms/client` is the engine every renderer shares, Razor's `forms.js` included: `forms.ts` is only a DOM layer that feeds the engine the named inputs' values (also those that aren't fields, such as the honeypot) and writes its state back as `data-sf-*` attributes, and `window.SproutForms` registers into the engine's registries. `createFormEngine` (`src/engine.ts`, with a `FormTransport`) holds one form's state (values, variables, errors, page index, status, outcome), replaces it on every change and tells its subscribers. Values are set by path (`paths.ts`, such as `people[0].email`), repeater entries are added and removed through it, and `next()` checks a page in the browser and with `validatePage` before moving on; it never touches the DOM, so it runs during SSR. Validators, submission guards and outcome handlers live in `Registry`s: the module-level `register*` functions fill the global ones, and a renderer creates children of those per app (per request on a server) and passes them to the engine, `validateForm`, `client.submit` and `handleOutcome`.

`@sproutforms/vue` renders the engine's state. `createSproutForms` is the per-app plugin: client, registries, `navigate`, `loadDefinition` and the themes. `<SproutForm>` loads a definition and renders `<SproutFormView>`, which creates the engine, provides the form context (`useSproutFormContext()`) and renders the theme's `Form`. `useField(field)` derives one field's state, its resolved control and the props to bind on it from that context and the field scope (`scope.ts`): the built-in `RepeaterEntry` calls `provideEntryScope`, so fields in an entry get their path and see the entry's values over the form's; the built-in `Field` is only layout on top of it, and custom wrappers should be too. Components resolve per form: a field's control from the form's `fields` prop by alias, then the form's theme, then the `default` theme, then `builtInFields`; the building blocks (`Form`, `Rows`, `Field`, `Actions`, `Errors`, `SubmissionGuard`, `Success`) from the theme, the `default` theme, then `builtInComponents`. The built-in components render the Razor views' classes and state attributes, and the package build copies `forms-layout.css` and `forms-default-theme.css` from `SproutForms.Core`, so keep the markup of `Views/Forms` and `src/components` in step.

`@sproutforms/nuxt` creates the plugin per request with a server client (with the API key) or a browser client (through the proxy in `src/runtime/server/proxy.ts`, which only forwards the delivery API's endpoints), and loads definitions with `useAsyncData` so they come with the payload.

## Adding New Field Types

1. Create descriptor in `SproutForms.Umbraco.Core/Descriptors/Fields/`
2. Create config model in `SproutForms.Core/Flows/Configs/`
3. Create view in `SproutForms.Core/Views/Forms/Fields/`
4. Override `GetClientConfiguration` when the config holds settings the browser mustn't see, and add its config to `BuiltInFieldConfigurations` in `src/SproutForms.Client/src/types.ts`
5. Register the field type in `AddSproutForms` (`SproutForms.Core/SproutFormsServiceCollectionExtensions.cs`) and the descriptor in `SproutFormComposer.cs`
6. Add its Vue control to `src/SproutForms.Vue/src/components/fields/` and `builtInFields`, with the same markup as its Razor view

## Field Groups (repeaters)

A field type whose configuration implements `IFormFieldGroupConfiguration` holds fields of its own, laid out in rows like a page (`IFormLayout`, which `FormPage` implements too). `RepeaterFieldType` (`repeater`) is the only one: the visitor fills in its entries as often as its min and max allow.

- Its fields live in the group's configuration, not in `FormDefinition.Fields`; `GetAllFields()` and `FindField(alias)` see every depth. Aliases are unique across the whole form. `FormDefinitionStructureValidator` checks the group's own layout, allows one level (`MaxFieldGroupDepth`), and only lets a child's conditions use its siblings and the fields its group may use; nothing outside a group may use a child.
- The value is a list of entries, each an object keyed by child alias. Razor inputs, uploads and errors are named by path, such as `people[0].firstName` (`FieldPath`); `SubmittedFieldValues.FromPostedForm` builds the entries from a posted form. `FormSubmissionService` parses what was submitted once into `SubmittedValues`, which keeps only the form's own fields (a group's value as its entries, never a posted value for an upload) and is the only place that checks the shape of submitted JSON; it is turned back into JSON once, to store.
- `FormSubmissionService` validates each entry with its conditions seeing the entry's values over the form's, drops the entries the visitor left empty (a value that fails a required check, like an unticked checkbox, counts as empty), keeps the posted index in error keys, and saves the rest. The group's own type only checks the number of entries, even when there are none.
- `IFormFieldType.GetDisplayValue` shows a value as text for workflows through `FormValueFormatter`; `WorkflowMessageResolver` and the email workflow use it. A group has one `{alias}` token; its children have none.
- The client model and the backoffice model carry a group's `rows` (and, in the backoffice, `fields`). The Razor view is `Fields/repeater.cshtml` with `RepeaterEntry.cshtml`; forms.js clones its `<template>` and renumbers the entries.

## Calculations

A definition has `Variables` (`FormVariable`: a number or text with an initial value and decimals), `Calculations` (`CalculationRule`s: an optional condition, a variable, an operation, a `CalculationOperand` that is a value, a field or a variable, and the `OwnerFieldAlias` of the field the backoffice lists it on) and `ConditionalOutcomes`, all in `SproutForms.Core/Models/Calculations/` and `Models/ConditionalOutcome.cs`. The user guide is [`docs/forms/calculations.md`](../docs/forms/calculations.md).

- A field's `Rules` (`FieldRule`: a condition and Show, Hide or Require) decide whether it's shown and required: hidden while a Hide rule holds, with Show rules only shown while one holds. `ConditionEvaluator.IsVisible`/`IsRequired` and `isShownByRules`/`isRequiredByRules` in `conditions.ts` implement this; the browser gets them with the client model. A rule that changes a variable is a `CalculationRule` with `OwnerFieldAlias` set.
- A `ConditionRule` reads a field (`FieldAlias`) or a variable (`VariableAlias`), and compares it with its `Value` as written or, by `ValueSource`, with the field or variable that `Value` names. `IConditionEvaluator` takes the variables next to the values; every condition anywhere can use them.
- `FormCalculator` runs the rules top to bottom with hidden fields (their own conditions or a skipped page) left out, and repeats until nothing changes when page or field conditions use variables. `FormSubmissionService` calls it before validating, since conditions can use variables, stores the result in `FormSubmission.Variables`, and passes it to the form type in `FormTypeSubmissionContext.Variables`.
- `SproutForms.Client/src/calculations.ts` is the same engine for the browser (`calculateVariables`, which the form engine calls for headless and Razor forms alike). Keep it in step with `FormCalculator` and `VariableValues`, as with the conditions.
- `CalculationDependencies` says which fields and variables conditions and rules read. `FormClientModelBuilder` uses it to send only the variables a page condition or field rule needs, and the variables their rules read, with their rules; the rest never leaves the server. A headless submit returns the same variables. `FormDefinitionStructureValidator` uses it to reject unknown references, rules that don't fit their variable's type, conditions whose variables depend on a later page, and a field whose visibility depends on itself.
- `FormSubmitOutcomeRunner.GetOutcome` picks the first conditional outcome whose condition holds, else `SubmitOutcome`. `VariableTokens` fills in `{var:alias}` in the message and redirect outcomes (HTML- and URL-encoded), the email subject and `WorkflowMessageResolver`.
- Code-first: `FormBuilder.Variable`, `Calculate`, `SetOutcomeWhen`, `ValueOf.Field` / `ValueOf.Variable`, and `ConditionBuilder.Field(alias).Is(...)` / `Variable(alias)`.

## Form Definition Types

A form type (`IFormDefinitionType`, in `SproutForms.Core/Models/FormTypes/`) says what kind of form a definition is, such as standard, quiz or poll. The user guide is [`docs/extending/form-types.md`](../docs/extending/form-types.md), and `SproutForms.Site/Examples/` has a working quiz, poll and product finder. It is stored per version in `FormDefinition.Type`, together with the type's typed settings. Definitions stored before form types existed read as `standard`. A form's type is chosen when it is created and can't be changed afterwards.

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

`SproutForms.Core.Tests` (NUnit, NSubstitute) covers Core: run `dotnet test src/SproutForms.Core.Tests`. To verify a change end-to-end in a running site (throwaway SQLite database, emails captured to disk), follow the `verify-in-site` skill in `.claude/skills/verify-in-site/SKILL.md`. It uses the `AiTest` launch profile of `SproutForms.Site`.
