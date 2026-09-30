using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Core.Services
{
    public class FormSubmissionService : IFormSubmissionService
    {
        private readonly IFormSubmissionRepository _submissions;
        private readonly IFormFieldType[] _fieldTypes;
        private readonly IFormDefinitionType[] _formTypes;
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
            IEnumerable<IFormDefinitionType> formTypes,
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
            _formTypes = formTypes.ToArray();
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
            var values = SubmittedValues.Parse(formVersion.Definition.Fields, request.Values);
            var storedFiles = new List<StoredFileReference>();

            try
            {
                await StoreFilesAsync(formVersion, files, values, storedFiles, errors);
                ValidateFields(formVersion, formVersion.Definition.Fields, values, errors);

                // What is stored, and what the form's type, workflows and outcome see
                var storedValues = values.ToDictionary();
                if (errors.Count != 0)
                {
                    await DeleteStoredFilesAsync(storedFiles);
                    return new FormSubmissionResult
                    {
                        Errors = errors,
                        Values = storedValues
                    };
                }

                var typeResult = await ProcessWithFormTypeAsync(formVersion, storedValues);
                if (typeResult.Errors.Count != 0)
                {
                    await DeleteStoredFilesAsync(storedFiles);
                    return new FormSubmissionResult
                    {
                        Errors = typeResult.Errors,
                        Values = storedValues
                    };
                }

                var httpContext = _httpContextAccessor.HttpContext;
                var submission = new FormSubmission
                {
                    Id = Guid.NewGuid(),
                    FormVersionId = formVersion.Id,
                    SubmittedAt = DateTime.UtcNow,
                    Values = storedValues,
                    Results = typeResult.Results.ToDictionary(it => it.Key, it => JsonSerializer.SerializeToElement(it.Value)),
                    PageUrl = GetPageUrl(request.PageUrl, httpContext),
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
                    Values = storedValues,
                    Submission = submission
                };
            }
            catch
            {
                await DeleteStoredFilesAsync(storedFiles);
                throw;
            }
        }

        // The page URL is posted by the client, so it is only kept when it is on this site or on a front-end the headless API allows
        private string? GetPageUrl(string? pageUrl, HttpContext? httpContext)
        {
            if (httpContext is null)
                return null;

            var headless = _options.CurrentValue.Headless;
            return SameHostUrl.GetOrNull(pageUrl, httpContext.Request)
                ?? (headless.Enabled ? AllowedOriginUrl.GetOrNull(pageUrl, headless.AllowedOrigins) : null);
        }

        // A form whose type is no longer registered is still accepted, without the type's processing
        private async Task<FormTypeSubmissionResult> ProcessWithFormTypeAsync(FormVersion formVersion, Dictionary<string, JsonElement> values)
        {
            var typeAlias = formVersion.Definition.Type.TypeAlias;
            var formType = _formTypes.FirstOrDefault(it => it.Alias == typeAlias);
            if (formType is null)
            {
                _logger.LogWarning("Form {FormId} uses form type {FormTypeAlias}, which is not registered; its submission is saved without the type's processing", formVersion.FormId, typeAlias);
                return FormTypeSubmissionResult.None;
            }

            return await formType.ProcessSubmissionAsync(new FormTypeSubmissionContext
            {
                Version = formVersion,
                Values = values
            }, CancellationToken.None);
        }

        public IReadOnlyDictionary<string, List<string>> ValidatePage(FormVersion formVersion, int pageIndex, IReadOnlyDictionary<string, JsonElement> values)
        {
            var page = formVersion.Definition.Pages[pageIndex];
            var aliases = page.Rows.SelectMany(row => row.Columns).Select(column => column.FieldAlias).ToHashSet();
            var fields = formVersion.Definition.Fields.Where(field => aliases.Contains(field.Alias));

            var errors = new Dictionary<string, List<string>>();
            ValidateFields(formVersion, fields, SubmittedValues.Parse(formVersion.Definition.Fields, values), errors, includeFiles: false);
            return errors;
        }

        private void ValidateFields(FormVersion formVersion, IEnumerable<FormField> fields, SubmittedValues values, Dictionary<string, List<string>> errors, bool includeFiles = true)
        {
            var conditionValues = values.ToDictionary();

            // The visitor skipped these pages, so their fields aren't validated, like a hidden field
            var skippedFieldAliases = formVersion.Definition.Pages
                .Where(page => !_conditionEvaluator.IsVisible(page, conditionValues))
                .SelectMany(page => page.Rows)
                .SelectMany(row => row.Columns)
                .Select(column => column.FieldAlias)
                .ToHashSet();

            var scope = new ValidationScope(formVersion, errors, includeFiles);
            ValidateScope(scope, fields.Where(field => !skippedFieldAliases.Contains(field.Alias)), values, conditionValues, string.Empty);
        }

        /// <summary>
        /// Validates the fields of the form, or of one entry of a field group. An entry's values are what its own fields hold, and
        /// its conditions see those over the form's values, so a condition can use a field of the same entry or of the form.
        /// </summary>
        private void ValidateScope(ValidationScope scope, IEnumerable<FormField> fields, SubmittedValues values, Dictionary<string, JsonElement> conditionValues, string pathPrefix)
        {
            foreach (var field in fields)
            {
                var path = pathPrefix + field.Alias;

                // A rejected upload already has its error; don't add "Field is required." on top of it
                if (scope.Errors.ContainsKey(path))
                    continue;

                // Uploads are left to the final submit, which receives the files
                if (!scope.IncludeFiles && field.Configuration is FileFieldConfig)
                    continue;

                if (!_conditionEvaluator.IsVisible(field, conditionValues))
                    continue;

                var isRequired = field.Required ||
                    _conditionEvaluator.IsRequired(field, conditionValues);

                // Fail closed: a field whose type is no longer registered can't be validated
                var fieldType = GetFieldType(scope.FormVersion, field);

                JsonElement rawValue;
                bool isEmpty;
                if (field.Configuration is IFormFieldGroupConfiguration group)
                {
                    // The entries the visitor left empty are dropped, and the rest are saved without them
                    var entries = ValidateEntries(scope, group, values.GetEntries(field.Alias), conditionValues, path);
                    values.SetEntries(field.Alias, entries);
                    rawValue = JsonSerializer.SerializeToElement(entries.Select(entry => entry.ToDictionary()));
                    isEmpty = entries.Count == 0;
                }
                else
                {
                    values.TryGetValue(field.Alias, out rawValue);
                    isEmpty = IsEmpty(rawValue);
                }
                if (isRequired && isEmpty)
                {
                    AddError(scope.Errors, path, FormTexts.Required);
                    continue;
                }

                if (isRequired && fieldType is IFormTypeRequiredHandler requiredHandler) // Additional required logic for checkboxes
                {
                    var result = requiredHandler.CheckForRequired(rawValue.ToString() ?? string.Empty);
                    if (!result.IsValid)
                    {
                        foreach (var error in result.Errors)
                        {
                            AddError(scope.Errors, path, error);
                        }
                    }
                }

                // A field group without entries still has to have as many as its minimum asks for
                if (isEmpty && field.Configuration is not IFormFieldGroupConfiguration)
                    continue;

                var validationResult = fieldType.Validate(
                    rawValue,
                    field.Configuration);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        AddError(scope.Errors, path, error);
                    }
                }
            }
        }

        // An entry's errors use its index as it was posted, so the front-end shows them on the entry the visitor sees
        private List<SubmittedValues> ValidateEntries(ValidationScope scope, IFormFieldGroupConfiguration group, List<SubmittedValues> entries, Dictionary<string, JsonElement> conditionValues, string path)
        {
            var kept = new List<SubmittedValues>();
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var entryPath = FieldPath.ForEntry(path, index);

                // A rejected upload leaves the entry without its value, but the visitor didn't leave the entry empty
                var hasErrors = scope.Errors.Keys.Any(key => key.StartsWith(entryPath, StringComparison.Ordinal));
                if (!hasErrors && IsBlankEntry(scope.FormVersion, group, entry))
                    continue;

                var entryConditionValues = new Dictionary<string, JsonElement>(conditionValues);
                foreach (var (alias, entryValue) in entry.ToDictionary())
                    entryConditionValues[alias] = entryValue;

                ValidateScope(scope, group.Fields, entry, entryConditionValues, entryPath);
                kept.Add(entry);
            }

            return kept;
        }

        // A value that wouldn't do for a required field, such as an unticked checkbox, isn't something the visitor filled in
        private bool IsBlankEntry(FormVersion formVersion, IFormFieldGroupConfiguration group, SubmittedValues entry)
            => group.Fields.All(field =>
            {
                if (field.Configuration is IFormFieldGroupConfiguration childGroup)
                    return entry.GetEntries(field.Alias).All(child => IsBlankEntry(formVersion, childGroup, child));
                if (!entry.TryGetValue(field.Alias, out var value) || IsEmpty(value))
                    return true;
                return GetFieldType(formVersion, field) is IFormTypeRequiredHandler requiredHandler
                    && !requiredHandler.CheckForRequired(value.ToString() ?? string.Empty).IsValid;
            });

        private static bool IsEmpty(JsonElement value)
            => value.ValueKind switch
            {
                JsonValueKind.Undefined or JsonValueKind.Null => true,
                JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
                _ => false
            };

        private IFormFieldType GetFieldType(FormVersion formVersion, FormField field)
            => _fieldTypes.FirstOrDefault(it => it.Alias == field.FieldTypeAlias)
                ?? throw new InvalidOperationException($"Form '{formVersion.FormId}' has field '{field.Alias}' with field type '{field.FieldTypeAlias}', which is not registered.");

        private static void AddError(Dictionary<string, List<string>> errors, string fieldPath, string message)
        {
            if (!errors.TryGetValue(fieldPath, out var list))
            {
                list = new List<string>();
                errors[fieldPath] = list;
            }

            list.Add(message);
        }

        // An upload is named by its field's path, such as "cv" or, in a field group's entry, "people[0].cv"
        private async Task StoreFilesAsync(
            FormVersion formVersion,
            IReadOnlyList<IFormFile> files,
            SubmittedValues values,
            List<StoredFileReference> storedFiles,
            Dictionary<string, List<string>> errors)
        {
            foreach (var file in files)
            {
                if (!FieldPath.TryParse(file.Name, out var path)) continue;

                var field = FindField(formVersion.Definition.Fields, path);
                if (field?.Configuration is not FileFieldConfig config) continue;

                var key = path.ToString();
                if (file.Length > config.MaxFileSizeBytes)
                {
                    AddError(errors, key, $"File size exceeds the maximum allowed size of {config.MaxFileSizeBytes} bytes.");
                    continue;
                }

                if (config.AllowedExtensions is not null)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!config.AllowedExtensions.Contains(ext))
                    {
                        AddError(errors, key, "This file extension is not allowed.");
                        continue;
                    }
                }

                var storageProvider = _formFileStorageProviders
                    .FirstOrDefault(p => p.Alias == config.StorageProviderAlias);
                if (storageProvider is null)
                {
                    _logger.LogError("Field {FieldAlias} of form {FormId} uses file storage provider {StorageProviderAlias}, which is not registered", field.Alias, formVersion.FormId, config.StorageProviderAlias);
                    AddError(errors, key, "File upload is currently unavailable.");
                    continue;
                }

                var reference = await storageProvider.SaveAsync(file, CancellationToken.None);
                storedFiles.Add(reference);
                var target = values;
                foreach (var (alias, index) in path.Entries)
                    target = target.GetOrAddEntry(alias, index);
                target.SetValue(path.FieldAlias, JsonSerializer.SerializeToElement(JsonSerializer.Serialize(reference)));
            }
        }

        // The field a path leads to, through the field groups on the way
        private static FormField? FindField(IReadOnlyList<FormField> fields, FieldPath path)
        {
            foreach (var (alias, _) in path.Entries)
            {
                if (fields.FirstOrDefault(it => it.Alias == alias)?.Configuration is not IFormFieldGroupConfiguration group)
                    return null;
                fields = group.Fields;
            }
            return fields.FirstOrDefault(it => it.Alias == path.FieldAlias);
        }

        private sealed record ValidationScope(FormVersion FormVersion, Dictionary<string, List<string>> Errors, bool IncludeFiles);

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
