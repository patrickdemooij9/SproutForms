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
- **Form**, **FormField**, **FormColumn**, **FormRow** - Core form structure
- **FormSubmission** - Captured form entries
- **Flows**: `FormWorkflow`, `WorkflowExecution`, `WorkflowExecutionStatus`
- **Outcomes**: `ShowMessageOutcome`, `RedirectUrlOutcome`, `RedirectUmbracoPageOutcome`
- **Submission Guards**: `IFormSubmissionGuard`, `RecaptchaV3SubmissionGuard`

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

## Adding New Field Types

1. Create descriptor in `SproutForms.Umbraco.Core/Descriptors/Fields/`
2. Create config model in `SproutForms.Core/Flows/Configs/`
3. Create view in `SproutForms.Umbraco.Core/Views/Forms/Fields/`
4. Register in `SproutFormComposer.cs`

## Testing

Run tests with:
```bash
dotnet test
```
