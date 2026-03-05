using System.Text.Json;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Services
{
    public class WorkflowTemplateService
    {
        private readonly IWorkflowTemplateRepository _templateRepository;

        public WorkflowTemplateService(IWorkflowTemplateRepository templateRepository)
        {
            _templateRepository = templateRepository;
        }

        public void ResolveWorkflowConfiguration(FormWorkflow workflow)
        {
            if (workflow.TemplateId is not { } templateId)
                return;

            var template = _templateRepository.GetById(templateId);
            if (template is null)
                return;

            var workflowConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(workflow.Configuration));

            if (workflowConfig is null)
                return;

            var templateConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                template.Configuration.GetRawText());

            if (templateConfig is null)
                return;

            var mergedConfig = new Dictionary<string, JsonElement>();

            foreach (var templateKey in templateConfig.Keys)
            {
                if (templateConfig.TryGetValue(templateKey, out var templateValue))
                {
                    if (template.LockedFields.Contains(templateKey))
                    {
                        mergedConfig[templateKey] = templateValue;
                    }
                    else if (workflowConfig.TryGetValue(templateKey, out var workflowValue))
                    {
                        mergedConfig[templateKey] = workflowValue;
                    }
                    else
                    {
                        mergedConfig[templateKey] = templateValue;
                    }
                }
            }

            foreach (var workflowKey in workflowConfig.Keys)
            {
                if (!mergedConfig.ContainsKey(workflowKey))
                {
                    mergedConfig[workflowKey] = workflowConfig[workflowKey];
                }
            }

            workflow.Configuration = mergedConfig;
        }

        public void ResolveFormWorkflows(IEnumerable<FormWorkflow> workflows)
        {
            foreach (var workflow in workflows)
            {
                ResolveWorkflowConfiguration(workflow);
            }
        }

        public WorkflowTemplate? GetTemplate(Guid? templateId)
        {
            if (templateId is not { } id)
                return null;

            return _templateRepository.GetById(id);
        }
    }
}
