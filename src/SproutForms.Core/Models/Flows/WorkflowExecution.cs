using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models.Flows
{
    public class WorkflowExecution
    {
        public Guid Id { get; init; }
        public Guid SubmissionId { get; init; }

        public string WorkflowAlias { get; init; } = default!;
        public string WorkflowTypeAlias { get; init; } = default!;
        public string ConfigurationJson { get; init; } = default!;

        public int Order { get; init; }

        public WorkflowExecutionStatus Status { get; set; }

        public int AttemptCount { get; set; }
        public string? LastError { get; set; }

        public DateTime CreatedUtc { get; init; }
        public DateTime? NextAttemptUtc { get; init; }
        public DateTime? StartedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
    }

}
