using SproutForms.Core.Models;

namespace SproutForms.Core.Repositories
{
    public interface IFormSubmissionRepository
    {
        void Add(FormSubmission submission);

        // Also returns a submission in the recycle bin
        Task<FormSubmission> Get(Guid id);

        // Leave out the submissions in the recycle bin
        IReadOnlyList<FormSubmission> GetByForm(
            Guid formId,
            int skip,
            int take,
            out int totalCount
        );
        int Count(Guid formId);

        // Most recently trashed first
        IReadOnlyList<FormSubmission> GetTrashedByForm(Guid formId, int skip, int take, out int totalCount);

        // Every submission of the form, also the ones in the recycle bin
        IReadOnlyList<FormSubmission> GetAllByForm(Guid formId);

        // The ones of the ids that are submissions of the form, in the recycle bin or not
        IReadOnlyList<FormSubmission> GetByIds(Guid formId, IEnumerable<Guid> ids);

        // Of every form: the submissions that were moved to the recycle bin before the threshold
        IReadOnlyList<FormSubmission> GetTrashedBefore(DateTime threshold);

        IReadOnlyCollection<Guid> GetVersionIdsWithSubmissions(Guid formId);

        void MoveToRecycleBin(IEnumerable<Guid> ids, DateTime trashedAt, string trashedBy);
        void RestoreFromRecycleBin(IEnumerable<Guid> ids);
        void Delete(IEnumerable<Guid> ids);
        void DeleteAllByForm(Guid formId);
    }
}
