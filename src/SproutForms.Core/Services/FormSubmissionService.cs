using Microsoft.AspNetCore.Http;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Flows;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Services
{
    public class FormSubmissionService : IFormSubmissionService
    {
        private readonly IFormSubmissionRepository _submissions;
        private readonly IFormFieldType[] _fieldTypes;
        private readonly IConditionEvaluator _conditionEvaluator;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IFormFileStorageProvider[] _formFileStorageProviders;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FormSubmissionService(
            IFormSubmissionRepository submissions,
            IEnumerable<IFormFieldType> fieldTypes,
            IConditionEvaluator conditionEvaluator,
            IWorkflowExecutionRepository workflowExecutionRepository,
            IEnumerable<IFormFileStorageProvider> formFileStorageProviders,
            IHttpContextAccessor httpContextAccessor)
        {
            _submissions = submissions;
            _fieldTypes = fieldTypes.ToArray();
            _conditionEvaluator = conditionEvaluator;
            _workflowExecutionRepository = workflowExecutionRepository;
            _formFileStorageProviders = [..formFileStorageProviders];
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<FormSubmissionResult> SubmitAsync(
            FormVersion formVersion,
            FormSubmissionRequest request)
        {
            var errors = new Dictionary<string, List<string>>();

            foreach (var field in formVersion.Definition.Fields)
            {
                var isVisible = _conditionEvaluator.IsVisible(field, request.Values);
                if (!isVisible)
                    continue;

                var isRequired = field.Required ||
                    _conditionEvaluator.IsRequired(field, request.Values);

                request.Values.TryGetValue(field.Alias, out var rawValue);

                var isEmpty = string.IsNullOrWhiteSpace(rawValue.ToString());
                if (isRequired && isEmpty)
                {
                    AddError(errors, field.Alias, "Field is required.");
                    continue;
                }

                var fieldType = _fieldTypes.First(it => it.Alias == field.FieldTypeAlias);

                if (fieldType is IFormTypeRequiredHandler requiredHandler) // Additional required logic for checkboxes
                {
                    var result = requiredHandler.CheckForRequired(rawValue.ToString() ?? string.Empty);
                    if (!result.IsValid)
                    {
                        foreach (var error in result.Errors)
                        {
                            AddError(errors, field.Alias, error);
                        }
                    }
                }

                if (isEmpty)
                    continue;

                var validationResult = fieldType.Validate(
                    rawValue,
                    field.Configuration);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        AddError(errors, field.Alias, error);
                    }
                }
            }

            if (errors.Count != 0)
            {
                return new FormSubmissionResult
                {
                    Errors = errors,
                    Values = request.Values
                };
            }

            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormVersionId = formVersion.Id,
                SubmittedAt = DateTime.UtcNow,
                Values = request.Values,
                PageUrl = ValidatePageUrl(request.PageUrl)
            };

            _submissions.Add(submission);

            await _workflowExecutionRepository.EnqueueAsync(formVersion.Definition.Workflows, submission);
            return new FormSubmissionResult
            {
                Values = request.Values
            };
        }

        private static void AddError(Dictionary<string, List<string>> errors, string fieldAlias, string message)
        {
            if (!errors.TryGetValue(fieldAlias, out var list))
            {
                list = new List<string>();
                errors[fieldAlias] = list;
            }

            list.Add(message);
        }

        public async Task<FormFileSubmitResult> HandleFileSubmits(FormVersion formVersion, IFormFile[] formFiles)
        {
            var result = new FormFileSubmitResult();
            foreach (var file in formFiles)
            {
                var field = formVersion.Definition.Fields
                    .FirstOrDefault(f => f.Alias == file.Name);
                if (field is null) continue;

                var fieldType = _fieldTypes.First(it => it.Alias == field.FieldTypeAlias);
                if (fieldType is null) continue;

                var config = field.Configuration as FileFieldConfig;
                if (config is null) continue;

                if (file.Length > config.MaxFileSizeBytes)
                {
                    result.Errors.Add(field.Alias, [$"File size exceeds the maximum allowed size of {config.MaxFileSizeBytes} bytes."]);
                    continue;
                }

                if (config.AllowedExtensions is not null)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!config.AllowedExtensions.Contains(ext))
                    {
                        result.Errors.Add(field.Alias, ["This file extension is not allowed."]);
                        continue;
                    }
                }

                var storageProvider = _formFileStorageProviders
                    .FirstOrDefault(p => p.Alias == config.StorageProviderAlias);
                var reference = await storageProvider.SaveAsync(file, CancellationToken.None);

                result.Values.Add(field.Alias, JsonSerializer.Serialize(reference));
            }
            return result;
        }

        private string? ValidatePageUrl(string? pageUrl)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
                return null;

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return null;

            try
            {
                var requestHost = new Uri(pageUrl).Host;
                var currentHost = context.Request.Host.Host;

                if (string.Equals(requestHost, currentHost, StringComparison.OrdinalIgnoreCase))
                    return pageUrl;
            }
            catch
            {
                return pageUrl;
            }

            return null;
        }
    }

}
