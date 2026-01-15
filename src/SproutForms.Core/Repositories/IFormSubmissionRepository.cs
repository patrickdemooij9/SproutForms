using SproutForms.Core.Models;

namespace SproutForms.Core.Repositories
{
    public interface IFormSubmissionRepository
    {
        void Add(FormSubmission submission);

        Task<FormSubmission> Get(Guid id);
        IReadOnlyList<FormSubmission> GetByForm(
            Guid formId,
            int skip,
            int take,
            out int totalCount
        );
        int Count(Guid formId);
        void DeleteAllByForm(Guid formId);
    }
}
