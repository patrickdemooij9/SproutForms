using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SproutForms.Core;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.ClientModels;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Core.Models.ViewModels;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Text.Json;

namespace SproutForms.Umbraco.Core.Services
{
    /// <summary>
    /// Builds the Razor view model of a form from its <see cref="FormClientModel"/>, nested for the views, with the values and errors
    /// of a post without JavaScript that was sent back to the page.
    /// </summary>
    public class FormRenderingService
    {
        private readonly FormClientModelBuilder _clientModelBuilder;
        private readonly IFormRepository _forms;
        private readonly IFormSubmissionGuard? _formSubmissionGuard;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;
        private Dictionary<string, List<string>> _errors = [];
        private Dictionary<string, JsonElement> _values = [];

        public FormRenderingService(FormClientModelBuilder clientModelBuilder,
            IFormRepository forms,
            IFormSubmissionGuard? formSubmissionGuard,
            IHttpContextAccessor httpContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory)
        {
            _clientModelBuilder = clientModelBuilder;
            _forms = forms;
            _formSubmissionGuard = formSubmissionGuard;
            _httpContextAccessor = httpContextAccessor;
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
        }

        public RenderedFormViewModel Build(FormVersion version)
        {
            var form = _forms.GetById(version.FormId)
                ?? throw new InvalidOperationException($"Form version '{version.Id}' belongs to form '{version.FormId}', which doesn't exist.");
            return Build(form, version);
        }

        public RenderedFormViewModel Build(Form form, FormVersion version)
        {
            ReadFromTempData(version.FormId);
            var clientModel = _clientModelBuilder.Build(form, version);

            var submissionGuards = new List<FormSubmissionGuardViewModel>();
            if (clientModel.SubmissionGuard is not null)
            {
                submissionGuards.Add(new FormSubmissionGuardViewModel
                {
                    Alias = clientModel.SubmissionGuard.Alias,
                    Settings = clientModel.SubmissionGuard.Settings,
                    PartialViewPath = _formSubmissionGuard?.PartialViewPath
                });
            }

            return new RenderedFormViewModel
            {
                Id = clientModel.Id,
                Pages = clientModel.Pages.Select(page => BuildPage(page, version.Definition)).ToList(),
                SubmitLabel = clientModel.SubmitLabel,
                ShowProgress = clientModel.ShowProgress,
                SubmissionGuards = submissionGuards,
                Variables = clientModel.Variables,
                Calculations = clientModel.Calculations,
                HasErrors = _errors.Count > 0
            };
        }

        private void ReadFromTempData(Guid formId)
        {
            var tempData = _tempDataDictionaryFactory.GetTempData(_httpContextAccessor.HttpContext);

            if (tempData.TryGetValue($"{formId}:FormErrors", out var errorsRaw) && errorsRaw != null)
                _errors = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(errorsRaw.ToString()!)!;
            if (tempData.TryGetValue($"{formId}:FormValues", out var valuesRaw) && valuesRaw != null)
                _values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(valuesRaw.ToString()!)!;
        }

        private FormPageViewModel BuildPage(FormClientPage page, FormDefinition definition)
            => new()
            {
                Index = page.Index,
                Title = page.Title,
                ProgressLabel = page.ProgressLabel,
                Rows = BuildRows(page.Rows, definition.Fields, string.Empty),
                NextLabel = page.NextLabel,
                PreviousLabel = page.PreviousLabel,
                Visibility = page.Visibility
            };

        private List<FormRowViewModel> BuildRows(IReadOnlyList<FormClientRow> rows, IReadOnlyList<FormField> fields, string prefix)
            => rows.Select(row => new FormRowViewModel
            {
                Columns = row.Columns.Select(column => new FormColumnViewModel
                {
                    Width = column.Width,
                    // Razor views run on the server, so they get the whole configuration, not only what the browser may see
                    Field = BuildField(column.Field, fields.First(field => field.Alias == column.Field.Alias), prefix)
                }).ToList()
            }).ToList();

        private FormFieldViewModel BuildField(FormClientField clientField, FormField field, string prefix)
        {
            var path = prefix + field.Alias;
            var fieldViewModel = new FormFieldViewModel
            {
                Alias = path,
                Label = clientField.Label,
                Type = clientField.Type,
                Required = clientField.Required,
                RendersOwnLabel = clientField.RendersOwnLabel,
                Configuration = field.Configuration,
                Rules = clientField.Rules,
                ValidationRules = clientField.ValidationRules,
                Entries = field.Configuration is RepeaterFieldConfig repeater && clientField.Rows is { } rows
                    ? [.. Enumerable.Range(0, repeater.GetInitialItemCount()).Select(index => BuildEntry(repeater, rows, FieldPath.ForEntry(path, index), repeater.GetItemTitle(index)))]
                    : [],
                EntryTemplate = field.Configuration is RepeaterFieldConfig template && clientField.Rows is { } templateRows
                    ? BuildEntry(template, templateRows, $"{path}[{FormFieldGroupEntryViewModel.IndexPlaceholder}].", null)
                    : null
            };

            if (_errors.TryGetValue(path, out var errors) is true)
            {
                fieldViewModel.Errors = errors?.ToArray() ?? [];
            }
            if (prefix.Length == 0 && _values.TryGetValue(field.Alias, out var value) && value.ValueKind == JsonValueKind.String)
            {
                fieldViewModel.Value = value.GetString();
            }

            return fieldViewModel;
        }

        private FormFieldGroupEntryViewModel BuildEntry(RepeaterFieldConfig repeater, IReadOnlyList<FormClientRow> rows, string prefix, string? title)
            => new()
            {
                Prefix = prefix,
                Title = title,
                RemoveLabel = string.IsNullOrWhiteSpace(repeater.RemoveLabel) ? FormTexts.RemoveItem : repeater.RemoveLabel,
                Rows = BuildRows(rows, repeater.Fields, prefix)
            };
    }
}
