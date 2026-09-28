using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Moves a form's submissions to its recycle bin and back. A submission in the bin keeps its values, files and workflow
    /// state, and is only deleted for good from the bin, or when it has been there longer than the retention period.
    /// Every action is one entry in the form's history, however many submissions it covered.
    /// </summary>
    public class FormSubmissionRecycleBinService
    {
        private readonly IFormSubmissionRepository _formSubmissionRepository;
        private readonly IFormVersionRepository _formVersionRepository;
        private readonly IFormAuditRepository _formAuditRepository;
        private readonly FormDeletionService _formDeletionService;

        public FormSubmissionRecycleBinService(
            IFormSubmissionRepository formSubmissionRepository,
            IFormVersionRepository formVersionRepository,
            IFormAuditRepository formAuditRepository,
            FormDeletionService formDeletionService)
        {
            _formSubmissionRepository = formSubmissionRepository;
            _formVersionRepository = formVersionRepository;
            _formAuditRepository = formAuditRepository;
            _formDeletionService = formDeletionService;
        }

        // Ids that aren't submissions of the form are left alone, in each of these
        public void MoveToRecycleBin(Guid formId, IEnumerable<Guid> submissionIds, string userKey)
        {
            var ids = _formSubmissionRepository.GetByIds(formId, submissionIds).Where(it => !it.IsTrashed).Select(it => it.Id).ToList();
            if (ids.Count == 0) return;

            var now = DateTime.UtcNow;
            _formSubmissionRepository.MoveToRecycleBin(ids, now, userKey);
            AddAudit(formId, FormAuditAction.SubmissionsMovedToRecycleBin, userKey, now, $"{Describe(ids.Count)} moved to the recycle bin");
        }

        public void Restore(Guid formId, IEnumerable<Guid> submissionIds, string userKey)
        {
            var ids = _formSubmissionRepository.GetByIds(formId, submissionIds).Where(it => it.IsTrashed).Select(it => it.Id).ToList();
            if (ids.Count == 0) return;

            _formSubmissionRepository.RestoreFromRecycleBin(ids);
            AddAudit(formId, FormAuditAction.SubmissionsRestoredFromRecycleBin, userKey, DateTime.UtcNow, $"{Describe(ids.Count)} restored from the recycle bin");
        }

        // Only submissions in the bin: a submission is always moved to the bin before it can be deleted for good
        public async Task DeletePermanentlyAsync(Guid formId, IEnumerable<Guid> submissionIds, string userKey)
        {
            var submissions = _formSubmissionRepository.GetByIds(formId, submissionIds).Where(it => it.IsTrashed).ToList();
            await DeleteAsync(formId, submissions, userKey);
        }

        public async Task EmptyAsync(Guid formId, string userKey)
        {
            var submissions = _formSubmissionRepository.GetTrashedByForm(formId, 0, int.MaxValue, out _);
            await DeleteAsync(formId, [.. submissions], userKey);
        }

        /// <summary>
        /// Deletes the submissions of every form that have been in the recycle bin longer than the retention period, and returns how many.
        /// </summary>
        public async Task<int> PurgeAsync(TimeSpan retention, string userKey)
        {
            var expired = _formSubmissionRepository.GetTrashedBefore(DateTime.UtcNow - retention);

            // Recorded per form, like the other actions; a version that's gone belongs to a form that is gone too
            foreach (var formSubmissions in expired.GroupBy(it => _formVersionRepository.Get(it.FormVersionId)?.FormId))
            {
                if (formSubmissions.Key is { } formId)
                {
                    await DeleteAsync(formId, [.. formSubmissions], userKey);
                }
                else
                {
                    await _formDeletionService.DeleteSubmissionsAsync([.. formSubmissions]);
                }
            }
            return expired.Count;
        }

        private async Task DeleteAsync(Guid formId, List<FormSubmission> submissions, string userKey)
        {
            if (submissions.Count == 0) return;

            await _formDeletionService.DeleteSubmissionsAsync(submissions);
            AddAudit(formId, FormAuditAction.SubmissionsDeleted, userKey, DateTime.UtcNow, $"{Describe(submissions.Count)} deleted permanently");
        }

        private static string Describe(int count) => count == 1 ? "1 submission" : $"{count} submissions";

        private void AddAudit(Guid formId, FormAuditAction action, string userKey, DateTime createdAt, string comment)
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
