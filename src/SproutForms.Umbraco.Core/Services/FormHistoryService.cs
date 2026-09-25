using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Umbraco.Core.Descriptors.Fields;
using SproutForms.Umbraco.Core.Descriptors.Flows;
using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using SproutForms.Umbraco.Core.Models.ViewModels;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Services;

namespace SproutForms.Umbraco.Core.Services
{
    /// <summary>
    /// A form's history in the backoffice: what happened to it, its versions, and rolling back to one of them.
    /// </summary>
    public class FormHistoryService
    {
        public const string SystemUserKey = "System";

        private readonly IFormRepository _formRepository;
        private readonly IFormVersionRepository _formVersionRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;
        private readonly IFormAuditRepository _formAuditRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IWorkflowTemplateRepository _templateRepository;
        private readonly IUserService _userService;
        private readonly FormVersionComparer _comparer;
        private readonly FormDefinitionTypeValidator _typeValidator;
        private readonly IFormDefinitionType[] _formTypes;
        private readonly IFormFieldType[] _fieldTypes;
        private readonly IFormWorkflowType[] _workflowTypes;
        private readonly IFormSubmitOutcomeType[] _outcomeTypes;
        private readonly IFormDefinitionTypeDescriptor[] _formTypeDescriptors;
        private readonly IFieldDescriptor[] _fieldDescriptors;
        private readonly IFlowDescriptor[] _flowDescriptors;
        private readonly IOutcomeDescriptor[] _outcomeDescriptors;

        public FormHistoryService(
            IFormRepository formRepository,
            IFormVersionRepository formVersionRepository,
            IFormSubmissionRepository formSubmissionRepository,
            IFormAuditRepository formAuditRepository,
            IFolderRepository folderRepository,
            IWorkflowTemplateRepository templateRepository,
            IUserService userService,
            FormVersionComparer comparer,
            FormDefinitionTypeValidator typeValidator,
            IEnumerable<IFormDefinitionType> formTypes,
            IEnumerable<IFormFieldType> fieldTypes,
            IEnumerable<IFormWorkflowType> workflowTypes,
            IEnumerable<IFormSubmitOutcomeType> outcomeTypes,
            IEnumerable<IFormDefinitionTypeDescriptor> formTypeDescriptors,
            IEnumerable<IFieldDescriptor> fieldDescriptors,
            IEnumerable<IFlowDescriptor> flowDescriptors,
            IEnumerable<IOutcomeDescriptor> outcomeDescriptors)
        {
            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
            _formSubmissionRepository = formSubmissionRepository;
            _formAuditRepository = formAuditRepository;
            _folderRepository = folderRepository;
            _templateRepository = templateRepository;
            _userService = userService;
            _comparer = comparer;
            _typeValidator = typeValidator;
            _formTypes = [.. formTypes];
            _fieldTypes = [.. fieldTypes];
            _workflowTypes = [.. workflowTypes];
            _outcomeTypes = [.. outcomeTypes];
            _formTypeDescriptors = [.. formTypeDescriptors];
            _fieldDescriptors = [.. fieldDescriptors];
            _flowDescriptors = [.. flowDescriptors];
            _outcomeDescriptors = [.. outcomeDescriptors];
        }

        /// <summary>
        /// Records what a backoffice save changed. The version is null when the definition didn't change.
        /// </summary>
        public void RecordSave(Form? previous, Form saved, FormVersion? version, string userKey)
        {
            var now = DateTime.UtcNow;
            if (previous is null)
            {
                Add(saved.Id, FormAuditAction.Created, userKey, now, version?.Id);
                return;
            }

            if (previous.Name != saved.Name)
            {
                Add(saved.Id, FormAuditAction.Renamed, userKey, now, comment: $"Renamed from '{previous.Name}' to '{saved.Name}'");
            }
            if (previous.Alias != saved.Alias)
            {
                Add(saved.Id, FormAuditAction.Renamed, userKey, now, comment: $"Alias changed from '{previous.Alias}' to '{saved.Alias}'");
            }
            if (previous.FolderId != saved.FolderId)
            {
                var folder = saved.FolderId is { } folderId ? _folderRepository.GetById(folderId) : null;
                Add(saved.Id, FormAuditAction.Moved, userKey, now, comment: folder is null ? "Moved to the root" : $"Moved to folder '{folder.Name}'");
            }
            if (version != null)
            {
                Add(saved.Id, FormAuditAction.Saved, userKey, now, version.Id);
            }
        }

        public async Task<PagedViewModel<FormAuditEntryBackofficeModel>> GetHistoryAsync(Guid formId, int skip, int take)
        {
            var entries = _formAuditRepository.GetByForm(formId, skip, take, out var total);
            var versionNumbers = _formVersionRepository.GetAll(formId).ToDictionary(it => it.Id, it => it.Version);
            var userNames = new Dictionary<string, string>();

            var items = new List<FormAuditEntryBackofficeModel>();
            foreach (var entry in entries)
            {
                items.Add(new FormAuditEntryBackofficeModel
                {
                    Id = entry.Id,
                    Action = entry.Action,
                    UserName = await GetUserNameAsync(entry.UserKey, userNames),
                    CreatedAt = AsUtc(entry.CreatedAt),
                    Version = entry.VersionId is { } versionId && versionNumbers.TryGetValue(versionId, out var number) ? number : null,
                    Comment = entry.Comment
                });
            }

            return new PagedViewModel<FormAuditEntryBackofficeModel>
            {
                Items = items,
                Total = total
            };
        }

        public async Task<IReadOnlyList<FormVersionBackofficeModel>> GetVersionsAsync(Guid formId)
        {
            var versions = _formVersionRepository.GetAll(formId);
            var userNames = new Dictionary<string, string>();

            var items = new List<FormVersionBackofficeModel>();
            foreach (var version in versions)
            {
                items.Add(await MapAsync(version, versions[0], userNames));
            }
            return items;
        }

        public async Task<FormVersionComparisonBackofficeModel?> CompareAsync(Guid formId, Guid versionId)
        {
            var form = _formRepository.GetById(formId);
            var version = _formVersionRepository.Get(versionId);
            var current = _formVersionRepository.GetLatest(formId);
            if (form is null || version is null || current is null || version.FormId != formId) return null;

            return new FormVersionComparisonBackofficeModel
            {
                Version = await MapAsync(version, current, []),
                Changes = _comparer.Compare(current.Definition, version.Definition),
                RemovedFieldsWithSubmissions = GetRemovedFieldsWithSubmissions(formId, current.Definition, version.Definition),
                RollbackErrors = GetRollbackErrors(form, version, current)
            };
        }

        /// <summary>
        /// Saves the definition of an older version as a new version, and returns why that isn't possible when it isn't.
        /// </summary>
        public IReadOnlyList<string> Rollback(Guid formId, Guid versionId, string userKey)
        {
            var form = _formRepository.GetById(formId);
            var version = _formVersionRepository.Get(versionId);
            var current = _formVersionRepository.GetLatest(formId);
            if (form is null || version is null || current is null || version.FormId != formId)
                return ["The form or version doesn't exist."];

            var errors = GetRollbackErrors(form, version, current);
            if (errors.Count > 0) return errors;

            var newVersion = new FormVersion
            {
                Id = Guid.NewGuid(),
                FormId = formId,
                Version = current.Version + 1,
                Status = FormStatus.Published,
                Definition = version.Definition,
                DefinitionHash = FormDefinitionHasher.Hash(version.Definition),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userKey
            };
            _formVersionRepository.Add(newVersion);
            Add(formId, FormAuditAction.RolledBack, userKey, newVersion.CreatedAt, newVersion.Id, $"Rolled back to version {version.Version}");
            return [];
        }

        private List<string> GetRollbackErrors(Form form, FormVersion version, FormVersion current)
        {
            if (form.Source == FormSource.Code)
                return ["This form is defined in code, so it can't be rolled back."];
            if (version.Id == current.Id)
                return ["This is the current version."];
            if (FormDefinitionHasher.Hash(version.Definition) == FormDefinitionHasher.Hash(current.Definition))
                return ["This version is the same as the current version."];

            // The same checks as saving the form, so a rollback never stores a form the backoffice can't open
            var definition = version.Definition;
            var errors = new List<string>();
            var formTypeAlias = definition.Type.TypeAlias;
            if (!_formTypes.Any(it => it.Alias == formTypeAlias) || !_formTypeDescriptors.Any(it => it.FormTypeAlias == formTypeAlias))
                errors.Add($"The form type '{formTypeAlias}' is no longer registered.");

            foreach (var fieldTypeAlias in definition.Fields.Select(it => it.FieldTypeAlias).Distinct())
            {
                if (!_fieldTypes.Any(it => it.Alias == fieldTypeAlias) || !_fieldDescriptors.Any(it => it.FieldTypeAlias == fieldTypeAlias))
                    errors.Add($"The field type '{fieldTypeAlias}' is no longer registered.");
            }

            foreach (var workflow in definition.Workflows)
            {
                if (!_workflowTypes.Any(it => it.Alias == workflow.WorkflowTypeAlias) || !_flowDescriptors.Any(it => it.FlowTypeAlias == workflow.WorkflowTypeAlias))
                    errors.Add($"The workflow type '{workflow.WorkflowTypeAlias}' of workflow '{workflow.Alias}' is no longer registered.");
                if (workflow.TemplateId is { } templateId && _templateRepository.GetById(templateId) is null)
                    errors.Add($"The template of workflow '{workflow.Alias}' has been deleted.");
            }

            var outcomeAlias = definition.SubmitOutcome.OutcomeTypeAlias;
            if (!_outcomeTypes.Any(it => it.Alias == outcomeAlias) || !_outcomeDescriptors.Any(it => it.OutcomeTypeAlias == outcomeAlias))
                errors.Add($"The submit outcome '{outcomeAlias}' is no longer registered.");

            // The validators assume the types they check are registered
            if (errors.Count > 0) return errors;

            errors.AddRange(FormDefinitionStructureValidator.Validate(definition));
            errors.AddRange(_typeValidator.Validate(definition));
            return errors;
        }

        // A field has submitted values when a submission was made with any version that has the field
        private List<string> GetRemovedFieldsWithSubmissions(Guid formId, FormDefinition current, FormDefinition version)
        {
            var removed = current.Fields.Where(field => !version.Fields.Any(it => it.Alias.Equals(field.Alias, StringComparison.OrdinalIgnoreCase))).ToList();
            if (removed.Count == 0) return [];

            var versionIdsWithSubmissions = _formSubmissionRepository.GetVersionIdsWithSubmissions(formId).ToHashSet();
            var aliasesWithSubmissions = _formVersionRepository.GetAll(formId)
                .Where(it => versionIdsWithSubmissions.Contains(it.Id))
                .SelectMany(it => it.Definition.Fields.Select(field => field.Alias))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return [.. removed.Where(field => aliasesWithSubmissions.Contains(field.Alias)).Select(field => field.Label)];
        }

        private async Task<FormVersionBackofficeModel> MapAsync(FormVersion version, FormVersion current, Dictionary<string, string> userNames)
        {
            return new FormVersionBackofficeModel
            {
                Id = version.Id,
                Version = version.Version,
                CreatedAt = AsUtc(version.CreatedAt),
                CreatedByName = await GetUserNameAsync(version.CreatedBy, userNames),
                IsCurrent = version.Id == current.Id
            };
        }

        // Stored as UTC, but the database gives the value back without its kind, which would be sent as local time
        private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private async Task<string> GetUserNameAsync(string userKey, Dictionary<string, string> userNames)
        {
            if (userNames.TryGetValue(userKey, out var cached)) return cached;

            var name = userKey switch
            {
                SystemUserKey => "System",
                _ when Guid.TryParse(userKey, out var key) => (await _userService.GetAsync(key))?.Name ?? "Deleted user",
                _ => "Unknown user"
            };
            userNames[userKey] = name;
            return name;
        }

        private void Add(Guid formId, FormAuditAction action, string userKey, DateTime createdAt, Guid? versionId = null, string? comment = null)
        {
            _formAuditRepository.Add(new FormAuditEntry
            {
                FormId = formId,
                Action = action,
                UserKey = userKey,
                CreatedAt = createdAt,
                VersionId = versionId,
                Comment = comment
            });
        }
    }
}
