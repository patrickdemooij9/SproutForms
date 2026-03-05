using SproutForms.Core.Models;

namespace SproutForms.Core.Repositories
{
    public interface IWorkflowTemplateRepository
    {
        WorkflowTemplate? GetById(Guid id);
        IReadOnlyList<WorkflowTemplate> GetAll();
        IReadOnlyList<WorkflowTemplate> GetByWorkflowType(string workflowTypeAlias);
        Guid Save(WorkflowTemplate template);
        void Delete(Guid id);
    }
}
