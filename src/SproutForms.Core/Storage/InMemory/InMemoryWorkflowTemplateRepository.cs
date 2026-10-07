using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Storage.InMemory
{
    public class InMemoryWorkflowTemplateRepository : IWorkflowTemplateRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryWorkflowTemplateRepository(InMemoryStore store)
        {
            _store = store;
        }

        public WorkflowTemplate? GetById(Guid id)
        {
            lock (_store.Lock)
            {
                return _store.Templates.GetValueOrDefault(id);
            }
        }

        public IReadOnlyList<WorkflowTemplate> GetAll()
        {
            lock (_store.Lock)
            {
                return _store.Templates.Values.OrderBy(it => it.Name).ToList();
            }
        }

        public IReadOnlyList<WorkflowTemplate> GetByWorkflowType(string workflowTypeAlias)
        {
            lock (_store.Lock)
            {
                return _store.Templates.Values.Where(it => it.WorkflowTypeAlias == workflowTypeAlias).OrderBy(it => it.Name).ToList();
            }
        }

        public Guid Save(WorkflowTemplate template)
        {
            lock (_store.Lock)
            {
                if (template.Id == Guid.Empty)
                {
                    template.Id = Guid.NewGuid();
                    template.CreatedAt = DateTime.UtcNow;
                }
                template.UpdatedAt = DateTime.UtcNow;
                _store.Templates[template.Id] = template;
                return template.Id;
            }
        }

        public void Delete(Guid id)
        {
            lock (_store.Lock)
            {
                _store.Templates.Remove(id);
            }
        }
    }
}
