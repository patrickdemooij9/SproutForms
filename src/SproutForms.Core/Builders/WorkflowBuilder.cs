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
                WorkflowTypeAlias = "email", //TODO: Make this work better, const perhaps?
                Order = order
            });
            return this;
        }

        internal IReadOnlyList<FormWorkflow> Build()
            => _workflows;
    }

}
