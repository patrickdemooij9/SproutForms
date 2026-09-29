using Microsoft.Extensions.Logging;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Deletes a form with everything that belongs to it: versions, history, submissions, their workflow executions and their uploaded files.
    /// Also deletes single submissions that way.
    /// </summary>
    public class FormDeletionService
    {
        private readonly IFormRepository _formRepository;
        private readonly IFormVersionRepository _formVersionRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IFormAuditRepository _formAuditRepository;
        private readonly IFormFileStorageProvider[] _fileStorageProviders;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly ILogger<FormDeletionService> _logger;

        public FormDeletionService(
            IFormRepository formRepository,
            IFormVersionRepository formVersionRepository,
            IFormSubmissionRepository formSubmissionRepository,
            IWorkflowExecutionRepository workflowExecutionRepository,
            IFormAuditRepository formAuditRepository,
            IEnumerable<IFormFileStorageProvider> fileStorageProviders,
            IUnitOfWorkProvider unitOfWorkProvider,
            ILogger<FormDeletionService> logger)
        {
            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
            _formSubmissionRepository = formSubmissionRepository;
            _workflowExecutionRepository = workflowExecutionRepository;
            _formAuditRepository = formAuditRepository;
            _fileStorageProviders = [.. fileStorageProviders];
            _unitOfWorkProvider = unitOfWorkProvider;
            _logger = logger;
        }

        public async Task DeleteAsync(Guid formId)
        {
            // Collected before the rows go; the files are only deleted once the database delete has committed. The submissions in
            // the recycle bin still have theirs
            var files = GetUploadedFiles(_formSubmissionRepository.GetAllByForm(formId));

            using (var unitOfWork = _unitOfWorkProvider.Begin())
            {
                await _workflowExecutionRepository.DeleteAllByForm(formId);
                _formSubmissionRepository.DeleteAllByForm(formId);
                _formVersionRepository.DeleteAllByForm(formId);
                _formAuditRepository.DeleteAllByForm(formId);
                _formRepository.Delete(formId);
                unitOfWork.Complete();
            }

            await DeleteFilesAsync(files);
        }

        public async Task DeleteSubmissionsAsync(IReadOnlyCollection<FormSubmission> submissions)
        {
            if (submissions.Count == 0) return;

            var files = GetUploadedFiles(submissions);
            var ids = submissions.Select(it => it.Id).ToArray();

            using (var unitOfWork = _unitOfWorkProvider.Begin())
            {
                await _workflowExecutionRepository.DeleteBySubmissions(ids);
                _formSubmissionRepository.Delete(ids);
                unitOfWork.Complete();
            }

            await DeleteFilesAsync(files);
        }

        private async Task DeleteFilesAsync(IEnumerable<StoredFileReference> files)
        {
            foreach (var file in files)
            {
                var provider = _fileStorageProviders.FirstOrDefault(p => p.Alias == file.StorageProvider);
                if (provider is null)
                {
                    _logger.LogWarning("Could not delete file {FileId} of a deleted submission: storage provider {StorageProviderAlias} is not registered", file.Id, file.StorageProvider);
                    continue;
                }

                try
                {
                    await provider.DeleteAsync(file, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete file {FileId} of a deleted submission", file.Id);
                }
            }
        }

        private List<StoredFileReference> GetUploadedFiles(IEnumerable<FormSubmission> submissions)
        {
            var files = new List<StoredFileReference>();

            foreach (var versionSubmissions in submissions.GroupBy(s => s.FormVersionId))
            {
                var version = _formVersionRepository.Get(versionSubmissions.Key);
                if (version is null) continue;

                foreach (var submission in versionSubmissions)
                {
                    AddUploadedFiles(submission, version.Definition.Fields, submission.Values, files);
                }
            }

            return files;
        }

        // The uploads of the form's file fields, and of the file fields in each entry of its field groups
        private void AddUploadedFiles(FormSubmission submission, IReadOnlyList<FormField> fields, IReadOnlyDictionary<string, JsonElement> values, List<StoredFileReference> files)
        {
            foreach (var field in fields)
            {
                if (!values.TryGetValue(field.Alias, out var value))
                    continue;

                if (field.Configuration is IFormFieldGroupConfiguration group && value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in value.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object))
                    {
                        AddUploadedFiles(submission, group.Fields, entry.EnumerateObject().ToDictionary(it => it.Name, it => it.Value), files);
                    }
                    continue;
                }

                if (field.Configuration is not FileFieldConfig || value.ValueKind != JsonValueKind.String)
                    continue;

                try
                {
                    var reference = JsonSerializer.Deserialize<StoredFileReference>(value.GetString()!);
                    if (reference is not null)
                        files.Add(reference);
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Submission {SubmissionId} has an unreadable file reference in field {FieldAlias}", submission.Id, field.Alias);
                }
            }
        }
    }
}
