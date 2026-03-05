using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models.Flows
{
    public interface IFormWorkflowType
    {
        string Alias { get; }
        Type ConfigurationType { get; }

        object GetDefaultConfiguration();

        Task<WorkflowExecutionResult> ExecuteAsync(
            WorkflowContext context,
            CancellationToken ct);
    }
}
