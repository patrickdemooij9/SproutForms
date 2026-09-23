using Microsoft.Extensions.Logging;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Deletes a form with everything that belongs to it: versions, submissions, their workflow executions and their uploaded files.
    /// </summary>
    public class FormDeletionService
    {
        private readonly IFormRepository _formRepository;
        private readonly IFormVersionRepository _formVersionRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IFormFileStorageProvider[] _fileStorageProviders;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly ILogger<FormDeletionService> _logger;

        public FormDeletionService(
            IFormRepository formRepository,
            IFormVersionRepository formVersionRepository,
            IFormSubmissionRepository formSubmissionRepository,
            IWorkflowExecutionRepository workflowExecutionRepository,
            IEnumerable<IFormFileStorageProvider> fileStorageProviders,
            IUnitOfWorkProvider unitOfWorkProvider,
            ILogger<FormDeletionService> logger)
        {
            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
            _formSubmissionRepository = formSubmissionRepository;
            _workflowExecutionRepository = workflowExecutionRepository;
            _fileStorageProviders = [.. fileStorageProviders];
            _unitOfWorkProvider = unitOfWorkProvider;
            _logger = logger;
        }

        public async Task DeleteAsync(Guid formId)
        {
            // Collected before the rows go; the files are only deleted once the database delete has committed
            var files = GetUploadedFiles(formId);

            using (var unitOfWork = _unitOfWorkProvider.Begin())
            {
                await _workflowExecutionRepository.DeleteAllByForm(formId);
                _formSubmissionRepository.DeleteAllByForm(formId);
                _formVersionRepository.DeleteAllByForm(formId);
                _formRepository.Delete(formId);
                unitOfWork.Complete();
            }

            foreach (var file in files)
            {
                var provider = _fileStorageProviders.FirstOrDefault(p => p.Alias == file.StorageProvider);
                if (provider is null)
                {
                    _logger.LogWarning("Could not delete file {FileId} of deleted form {FormId}: storage provider {StorageProviderAlias} is not registered", file.Id, formId, file.StorageProvider);
                    continue;
                }

                try
                {
                    await provider.DeleteAsync(file, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete file {FileId} of deleted form {FormId}", file.Id, formId);
                }
            }
        }

        private List<StoredFileReference> GetUploadedFiles(Guid formId)
        {
            var files = new List<StoredFileReference>();
            var submissions = _formSubmissionRepository.GetByForm(formId, 0, int.MaxValue, out _);

            foreach (var versionSubmissions in submissions.GroupBy(s => s.FormVersionId))
            {
                var version = _formVersionRepository.Get(versionSubmissions.Key);
                if (version is null) continue;

                var fileFieldAliases = version.Definition.Fields
                    .Where(f => f.Configuration is FileFieldConfig)
                    .Select(f => f.Alias)
                    .ToArray();

                foreach (var submission in versionSubmissions)
                {
                    foreach (var alias in fileFieldAliases)
                    {
                        if (!submission.Values.TryGetValue(alias, out var value) || value.ValueKind != JsonValueKind.String)
                            continue;

                        try
                        {
                            var reference = JsonSerializer.Deserialize<StoredFileReference>(value.GetString()!);
                            if (reference is not null)
                                files.Add(reference);
                        }
                        catch (JsonException)
                        {
                            _logger.LogWarning("Submission {SubmissionId} has an unreadable file reference in field {FieldAlias}", submission.Id, alias);
                        }
                    }
                }
            }

            return files;
        }
    }
}
