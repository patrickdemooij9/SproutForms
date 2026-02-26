using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SproutForms.Core.Models;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Core.Models.ViewModels;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SproutForms.Umbraco.Core.Services
{
    public class FormRenderingService
    {
        private readonly IFormFieldType[] _fieldTypes;
        private readonly IFormSubmissionGuard _formSubmissionGuard;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;
        private Dictionary<string, List<string>> _errors = [];
        private Dictionary<string, string> _values = [];

        public FormRenderingService(IEnumerable<IFormFieldType> fieldTypes,
            IFormSubmissionGuard formSubmissionGuard,
            IHttpContextAccessor httpContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory)
        {
            _fieldTypes = [.. fieldTypes];
            _formSubmissionGuard = formSubmissionGuard;
            _httpContextAccessor = httpContextAccessor;
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
        }

        public RenderedFormViewModel Build(FormVersion version)
        {
            ReadFromTempData(version.FormId);
            var submissionGuards = new List<FormSubmissionGuardViewModel>();
            if (_formSubmissionGuard is not null)
            {
                submissionGuards.Add(new FormSubmissionGuardViewModel
                {
                    Alias = _formSubmissionGuard.Alias,
                    Settings = _formSubmissionGuard.GetFrontendSettings()
                });
            }
            return new RenderedFormViewModel
            {
                Id = version.FormId,
                Rows = version.Definition.Rows.Select(it => BuildRow(it, version.Definition.Fields.ToArray())).ToList(),
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

        private FormRowViewModel BuildRow(FormRow row, FormField[] fields)
            => new()
            {
                Columns = row.Columns.Select(it => BuildColumn(it, fields)).ToList()
            };

        private FormColumnViewModel BuildColumn(FormColumn column, FormField[] fields)
        {
            var field = fields.First(it => it.Alias == column.FieldAlias);
            var fieldType = _fieldTypes.First(it => it.Alias == field.FieldTypeAlias);

            var fieldViewModel = new FormFieldViewModel
            {
                Alias = field.Alias,
                Label = field.Label,
                Type = fieldType.Alias,
                Required = field.Required,
                RendersOwnLabel = fieldType.RendersOwnLabel,
                Configuration = field.Configuration,
                Conditions = field.Conditions,
                ValidationRules = fieldType.GetValidationRules(field.Configuration),
            };

            if (_errors.TryGetValue(field.Alias, out var errors) is true)
            {
                fieldViewModel.Errors = errors?.ToArray() ?? [];
            }
            if (_values.TryGetValue(field.Alias, out var value) is true)
            {
                fieldViewModel.Value = value?.ToString();
            }

            return new FormColumnViewModel
            {
                Width = column.Width,
                Field = fieldViewModel
            };
        }
    }
}
