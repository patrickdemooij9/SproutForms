using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
        private Dictionary<string, string> _values = [];

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
                HasErrors = _errors.Count > 0
            };
        }

        private void ReadFromTempData(Guid formId)
        {
            var tempData = _tempDataDictionaryFactory.GetTempData(_httpContextAccessor.HttpContext);

            if (tempData.TryGetValue($"{formId}:FormErrors", out var errorsRaw) && errorsRaw != null)
                _errors = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(errorsRaw.ToString()!)!;
            if (tempData.TryGetValue($"{formId}:FormValues", out var valuesRaw) && valuesRaw != null)
                _values = JsonSerializer.Deserialize<Dictionary<string, string>>(valuesRaw.ToString()!)!;
        }

        private FormPageViewModel BuildPage(FormClientPage page, FormDefinition definition)
            => new()
            {
                Index = page.Index,
                Title = page.Title,
                ProgressLabel = page.ProgressLabel,
                Rows = page.Rows.Select(row => new FormRowViewModel
                {
                    Columns = row.Columns.Select(column => new FormColumnViewModel
                    {
                        Width = column.Width,
                        // Razor views run on the server, so they get the whole configuration, not only what the browser may see
                        Field = BuildField(column.Field, definition.Fields.First(field => field.Alias == column.Field.Alias))
                    }).ToList()
                }).ToList(),
                NextLabel = page.NextLabel,
                PreviousLabel = page.PreviousLabel,
                Visibility = page.Visibility
            };

        private FormFieldViewModel BuildField(FormClientField clientField, FormField field)
        {
            var fieldViewModel = new FormFieldViewModel
            {
                Alias = clientField.Alias,
                Label = clientField.Label,
                Type = clientField.Type,
                Required = clientField.Required,
                RendersOwnLabel = clientField.RendersOwnLabel,
                Configuration = field.Configuration,
                Conditions = clientField.Conditions,
                ValidationRules = clientField.ValidationRules,
            };

            if (_errors.TryGetValue(field.Alias, out var errors) is true)
            {
                fieldViewModel.Errors = errors?.ToArray() ?? [];
            }
            if (_values.TryGetValue(field.Alias, out var value) is true)
            {
                fieldViewModel.Value = value?.ToString();
            }

            return fieldViewModel;
        }
    }
}
