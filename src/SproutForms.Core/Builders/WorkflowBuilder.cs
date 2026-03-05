using SproutForms.Core.Builders.Flows;
using SproutForms.Core.Models.Flows;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

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

            var order = 0;
            if (_workflows.Count > 0)
            {
                order = _workflows.Min(it => it.Order) + 1;
            }

            _workflows.Add(new FormWorkflow
            {
                Alias = alias,
                Configuration = builder.Build(),
                WorkflowTypeAlias = "email",
                Order = order
            });
            return this;
        }

        public WorkflowBuilder SendToSlack(
            string alias,
            Action<SlackWorkflowConfigBuilder> configure)
        {
            var builder = new SlackWorkflowConfigBuilder();
            configure(builder);

            var order = 0;
            if (_workflows.Count > 0)
            {
                order = _workflows.Min(it => it.Order) + 1;
            }

            _workflows.Add(new FormWorkflow
            {
                Alias = alias,
                Configuration = builder.Build(),
                WorkflowTypeAlias = "slack",
                Order = order
            });
            return this;
        }

        public WorkflowBuilder SendToTeams(
            string alias,
            Action<TeamsWorkflowConfigBuilder> configure)
        {
            var builder = new TeamsWorkflowConfigBuilder();
            configure(builder);

            var order = 0;
            if (_workflows.Count > 0)
            {
                order = _workflows.Min(it => it.Order) + 1;
            }

            _workflows.Add(new FormWorkflow
            {
                Alias = alias,
                Configuration = builder.Build(),
                WorkflowTypeAlias = "teams",
                Order = order
            });
            return this;
        }

        public WorkflowBuilder PostToCustomEndpoint(
            string alias,
            Action<CustomPostWorkflowConfigBuilder> configure)
        {
            var builder = new CustomPostWorkflowConfigBuilder();
            configure(builder);

            var order = 0;
            if (_workflows.Count > 0)
            {
                order = _workflows.Min(it => it.Order) + 1;
            }

            _workflows.Add(new FormWorkflow
            {
                Alias = alias,
                Configuration = builder.Build(),
                WorkflowTypeAlias = "customPost",
                Order = order
            });
            return this;
        }

        internal IReadOnlyList<FormWorkflow> Build()
            => _workflows;
    }

}
