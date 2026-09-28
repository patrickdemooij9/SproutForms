using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Moves forms to the recycle bin and back. A form in the bin keeps its versions, history and submissions, and is only deleted for
    /// good from the bin, or when it has been there longer than the retention period.
    /// </summary>
    public class FormRecycleBinService
    {
        private readonly IFormRepository _formRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IFormAuditRepository _formAuditRepository;
        private readonly FormDeletionService _formDeletionService;

        public FormRecycleBinService(
            IFormRepository formRepository,
            IFolderRepository folderRepository,
            IFormAuditRepository formAuditRepository,
            FormDeletionService formDeletionService)
        {
            _formRepository = formRepository;
            _folderRepository = folderRepository;
            _formAuditRepository = formAuditRepository;
            _formDeletionService = formDeletionService;
        }

        /// <summary>
        /// Moves the forms to the recycle bin, or none of them when one can't be moved. Returns why, when it can't.
        /// </summary>
        public IReadOnlyList<string> MoveToRecycleBin(IEnumerable<Guid> formIds, string userKey)
        {
            // Ids that aren't forms, such as selected folders, are left alone
            var forms = GetForms(formIds).Where(it => !it.IsTrashed).ToList();

            // A code-first form would be registered again on the next start, as a new form without its history
            var errors = forms
                .Where(it => it.Source == FormSource.Code)
                .Select(it => $"'{it.Name}' is defined in code, so it can't be deleted in the backoffice.")
                .ToList();
            if (errors.Count > 0) return errors;

            var now = DateTime.UtcNow;
            foreach (var form in forms)
            {
                _formRepository.MoveToRecycleBin(form.Id, now, userKey);
                AddAudit(form.Id, FormAuditAction.MovedToRecycleBin, userKey, now);
            }
            return [];
        }

        public void Restore(IEnumerable<Guid> formIds, string userKey)
        {
            var now = DateTime.UtcNow;
            foreach (var form in GetForms(formIds).Where(it => it.IsTrashed))
            {
                // Back where it was, or in the root when its folder is gone
                var folderExists = form.FolderId is { } folderId && _folderRepository.GetById(folderId) != null;
                _formRepository.RestoreFromRecycleBin(form.Id, folderExists ? form.FolderId : null);
                AddAudit(form.Id, FormAuditAction.RestoredFromRecycleBin, userKey, now,
                    form.FolderId != null && !folderExists ? "Restored to the root, because its folder no longer exists" : null);
            }
        }

        // Only forms in the bin: a form is always moved to the bin before it can be deleted for good
        public async Task DeletePermanentlyAsync(IEnumerable<Guid> formIds)
        {
            foreach (var form in GetForms(formIds).Where(it => it.IsTrashed))
            {
                await _formDeletionService.DeleteAsync(form.Id);
            }
        }

        public async Task EmptyAsync()
        {
            foreach (var form in _formRepository.GetTrashed(0, int.MaxValue, out _))
            {
                await _formDeletionService.DeleteAsync(form.Id);
            }
        }

        /// <summary>
        /// Deletes the forms that have been in the recycle bin longer than the retention period, and returns how many.
        /// </summary>
        public async Task<int> PurgeAsync(TimeSpan retention)
        {
            var threshold = DateTime.UtcNow - retention;
            var expired = _formRepository.GetTrashed(0, int.MaxValue, out _).Where(it => it.TrashedAt < threshold).ToList();
            foreach (var form in expired)
            {
                await _formDeletionService.DeleteAsync(form.Id);
            }
            return expired.Count;
        }

        private IEnumerable<Form> GetForms(IEnumerable<Guid> formIds)
            => formIds.Distinct().Select(_formRepository.GetById).OfType<Form>();

        private void AddAudit(Guid formId, FormAuditAction action, string userKey, DateTime createdAt, string? comment = null)
        {
            _formAuditRepository.Add(new FormAuditEntry
            {
                FormId = formId,
                Action = action,
                UserKey = userKey,
                CreatedAt = createdAt,
                Comment = comment
            });
        }
    }
}
