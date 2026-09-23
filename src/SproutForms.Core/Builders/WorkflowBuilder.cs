using SproutForms.Core.Builders.Flows;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Core.Builders
{
    public class WorkflowBuilder
    {
        private readonly List<FormWorkflow> _workflows = [];

        public WorkflowBuilder SendEmail(
            string alias,
            Action<EmailWorkflowConfigBuilder> configure)
        {
            var builder = new EmailWorkflowConfigBuilder();
            configure(builder);
            return Add(alias, "email", builder.Build());
        }

        public WorkflowBuilder SendToSlack(
            string alias,
            Action<SlackWorkflowConfigBuilder> configure)
        {
            var builder = new SlackWorkflowConfigBuilder();
            configure(builder);
            return Add(alias, "slack", builder.Build());
        }

        public WorkflowBuilder SendToTeams(
            string alias,
            Action<TeamsWorkflowConfigBuilder> configure)
        {
            var builder = new TeamsWorkflowConfigBuilder();
            configure(builder);
            return Add(alias, "teams", builder.Build());
        }

        public WorkflowBuilder PostToCustomEndpoint(
            string alias,
            Action<CustomPostWorkflowConfigBuilder> configure)
        {
            var builder = new CustomPostWorkflowConfigBuilder();
            configure(builder);
            return Add(alias, "customPost", builder.Build());
        }

        // Workflows run in the order they are added: each one waits until every workflow with a lower order has succeeded
        private WorkflowBuilder Add(string alias, string workflowTypeAlias, object configuration)
        {
            _workflows.Add(new FormWorkflow
            {
                Alias = alias,
                Configuration = configuration,
                WorkflowTypeAlias = workflowTypeAlias,
                Order = _workflows.Count == 0 ? 0 : _workflows.Max(it => it.Order) + 1
            });
            return this;
        }

        internal IReadOnlyList<FormWorkflow> Build()
            => _workflows;
    }

}
