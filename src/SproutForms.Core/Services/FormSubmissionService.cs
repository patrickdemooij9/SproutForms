using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Repositories;
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
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptionsMonitor<SproutFormsOptions> _options;
        private readonly ILogger<FormSubmissionService> _logger;

        public FormSubmissionService(
            IFormSubmissionRepository submissions,
            IEnumerable<IFormFieldType> fieldTypes,
            IConditionEvaluator conditionEvaluator,
            IWorkflowExecutionRepository workflowExecutionRepository,
            IEnumerable<IFormFileStorageProvider> formFileStorageProviders,
            IUnitOfWorkProvider unitOfWorkProvider,
            IHttpContextAccessor httpContextAccessor,
            IOptionsMonitor<SproutFormsOptions> options,
            ILogger<FormSubmissionService> logger)
        {
            _submissions = submissions;
            _fieldTypes = fieldTypes.ToArray();
            _conditionEvaluator = conditionEvaluator;
            _workflowExecutionRepository = workflowExecutionRepository;
            _formFileStorageProviders = [..formFileStorageProviders];
            _unitOfWorkProvider = unitOfWorkProvider;
            _httpContextAccessor = httpContextAccessor;
            _options = options;
            _logger = logger;
        }

        public async Task<FormSubmissionResult> SubmitAsync(
            FormVersion formVersion,
            FormSubmissionRequest request,
            IReadOnlyList<IFormFile> files)
        {
            var errors = new Dictionary<string, List<string>>();
            var values = new Dictionary<string, JsonElement>(request.Values);
            var storedFiles = new List<StoredFileReference>();

            try
            {
                await StoreFilesAsync(formVersion, files, values, storedFiles, errors);
                ValidateFields(formVersion, values, errors);

                if (errors.Count != 0)
                {
                    await DeleteStoredFilesAsync(storedFiles);
                    return new FormSubmissionResult
                    {
                        Errors = errors,
                        Values = values
                    };
                }

                var httpContext = _httpContextAccessor.HttpContext;
                var submission = new FormSubmission
                {
                    Id = Guid.NewGuid(),
                    FormVersionId = formVersion.Id,
                    SubmittedAt = DateTime.UtcNow,
                    Values = values,
                    PageUrl = httpContext is null ? null : SameHostUrl.GetOrNull(request.PageUrl, httpContext.Request),
                    IpAddress = _options.CurrentValue.StoreIpAddress ? httpContext?.Connection.RemoteIpAddress?.ToString() : null
                };

                // The submission and its workflow queue are saved together, or not at all
                using (var unitOfWork = _unitOfWorkProvider.Begin())
                {
                    _submissions.Add(submission);
                    await _workflowExecutionRepository.EnqueueAsync(formVersion.Definition.Workflows, submission);
                    unitOfWork.Complete();
                }

                return new FormSubmissionResult
                {
                    Values = values
                };
            }
            catch
            {
                await DeleteStoredFilesAsync(storedFiles);
                throw;
            }
        }

        private void ValidateFields(FormVersion formVersion, Dictionary<string, JsonElement> values, Dictionary<string, List<string>> errors)
        {
            foreach (var field in formVersion.Definition.Fields)
            {
                // A rejected upload already has its error; don't add "Field is required." on top of it
                if (errors.ContainsKey(field.Alias))
                    continue;

                var isVisible = _conditionEvaluator.IsVisible(field, values);
                if (!isVisible)
                    continue;

                var isRequired = field.Required ||
                    _conditionEvaluator.IsRequired(field, values);

                values.TryGetValue(field.Alias, out var rawValue);

                var isEmpty = string.IsNullOrWhiteSpace(rawValue.ToString());
                if (isRequired && isEmpty)
                {
                    AddError(errors, field.Alias, "Field is required.");
                    continue;
                }

                // Fail closed: a field whose type is no longer registered can't be validated
                var fieldType = _fieldTypes.FirstOrDefault(it => it.Alias == field.FieldTypeAlias)
                    ?? throw new InvalidOperationException($"Form '{formVersion.FormId}' has field '{field.Alias}' with field type '{field.FieldTypeAlias}', which is not registered.");

                if (isRequired && fieldType is IFormTypeRequiredHandler requiredHandler) // Additional required logic for checkboxes
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

        private async Task StoreFilesAsync(
            FormVersion formVersion,
            IReadOnlyList<IFormFile> files,
            Dictionary<string, JsonElement> values,
            List<StoredFileReference> storedFiles,
            Dictionary<string, List<string>> errors)
        {
            foreach (var file in files)
            {
                var field = formVersion.Definition.Fields
                    .FirstOrDefault(f => f.Alias == file.Name);
                if (field?.Configuration is not FileFieldConfig config) continue;

                if (file.Length > config.MaxFileSizeBytes)
                {
                    AddError(errors, field.Alias, $"File size exceeds the maximum allowed size of {config.MaxFileSizeBytes} bytes.");
                    continue;
                }

                if (config.AllowedExtensions is not null)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!config.AllowedExtensions.Contains(ext))
                    {
                        AddError(errors, field.Alias, "This file extension is not allowed.");
                        continue;
                    }
                }

                var storageProvider = _formFileStorageProviders
                    .FirstOrDefault(p => p.Alias == config.StorageProviderAlias);
                if (storageProvider is null)
                {
                    _logger.LogError("Field {FieldAlias} of form {FormId} uses file storage provider {StorageProviderAlias}, which is not registered", field.Alias, formVersion.FormId, config.StorageProviderAlias);
                    AddError(errors, field.Alias, "File upload is currently unavailable.");
                    continue;
                }

                var reference = await storageProvider.SaveAsync(file, CancellationToken.None);
                storedFiles.Add(reference);
                values[field.Alias] = JsonSerializer.SerializeToElement(JsonSerializer.Serialize(reference));
            }
        }

        private async Task DeleteStoredFilesAsync(List<StoredFileReference> storedFiles)
        {
            foreach (var reference in storedFiles)
            {
                try
                {
                    var storageProvider = _formFileStorageProviders.First(p => p.Alias == reference.StorageProvider);
                    await storageProvider.DeleteAsync(reference, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete uploaded file {FileId} of a rejected submission", reference.Id);
                }
            }
        }
    }

}
