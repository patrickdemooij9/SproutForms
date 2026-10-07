using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Storage.InMemory
{
    public class InMemoryFormSubmissionRepository : IFormSubmissionRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryFormSubmissionRepository(InMemoryStore store)
        {
            _store = store;
        }

        public void Add(FormSubmission submission)
        {
            lock (_store.Lock)
            {
                _store.Submissions[submission.Id] = submission;
            }
        }

        public Task<FormSubmission> Get(Guid id)
        {
            lock (_store.Lock)
            {
                return Task.FromResult(_store.Submissions[id]);
            }
        }

        public IReadOnlyList<FormSubmission> GetByForm(Guid formId, int skip, int take, out int totalCount)
        {
            lock (_store.Lock)
            {
                var submissions = OfForm(formId).Where(it => !it.IsTrashed).OrderByDescending(it => it.SubmittedAt).ToList();
                totalCount = submissions.Count;
                return submissions.Skip(skip).Take(take).ToList();
            }
        }

        public int Count(Guid formId)
        {
            lock (_store.Lock)
            {
                return OfForm(formId).Count(it => !it.IsTrashed);
            }
        }

        public IReadOnlyList<FormSubmission> GetTrashedByForm(Guid formId, int skip, int take, out int totalCount)
        {
            lock (_store.Lock)
            {
                var submissions = OfForm(formId).Where(it => it.IsTrashed).OrderByDescending(it => it.TrashedAt).ToList();
                totalCount = submissions.Count;
                return submissions.Skip(skip).Take(take).ToList();
            }
        }

        public IReadOnlyList<FormSubmission> GetAllByForm(Guid formId)
        {
            lock (_store.Lock)
            {
                return OfForm(formId).ToList();
            }
        }

        public IReadOnlyList<FormSubmission> GetByIds(Guid formId, IEnumerable<Guid> ids)
        {
            var idSet = ids.ToHashSet();
            lock (_store.Lock)
            {
                return OfForm(formId).Where(it => idSet.Contains(it.Id)).ToList();
            }
        }

        public IReadOnlyList<FormSubmission> GetTrashedBefore(DateTime threshold)
        {
            lock (_store.Lock)
            {
                return _store.Submissions.Values.Where(it => it.TrashedAt < threshold).ToList();
            }
        }

        public IReadOnlyCollection<Guid> GetVersionIdsWithSubmissions(Guid formId)
        {
            lock (_store.Lock)
            {
                return OfForm(formId).Select(it => it.FormVersionId).ToHashSet();
            }
        }

        public void MoveToRecycleBin(IEnumerable<Guid> ids, DateTime trashedAt, string trashedBy)
        {
            lock (_store.Lock)
            {
                foreach (var submission in Find(ids))
                {
                    submission.TrashedAt = trashedAt;
                    submission.TrashedBy = trashedBy;
                }
            }
        }

        public void RestoreFromRecycleBin(IEnumerable<Guid> ids)
        {
            lock (_store.Lock)
            {
                foreach (var submission in Find(ids))
                {
                    submission.TrashedAt = null;
                    submission.TrashedBy = null;
                }
            }
        }

        public void Delete(IEnumerable<Guid> ids)
        {
            lock (_store.Lock)
            {
                foreach (var id in ids)
                {
                    _store.Submissions.Remove(id);
                }
            }
        }

        public void DeleteAllByForm(Guid formId)
        {
            lock (_store.Lock)
            {
                foreach (var submission in OfForm(formId).ToList())
                {
                    _store.Submissions.Remove(submission.Id);
                }
            }
        }

        // Call inside the lock
        private IEnumerable<FormSubmission> OfForm(Guid formId)
            => _store.Submissions.Values.Where(it => _store.GetFormId(it) == formId);

        // Call inside the lock
        private List<FormSubmission> Find(IEnumerable<Guid> ids)
            => ids.Select(id => _store.Submissions.GetValueOrDefault(id)).OfType<FormSubmission>().ToList();
    }
}
