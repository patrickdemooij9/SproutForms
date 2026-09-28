using SproutForms.Core.Models.Flows;
using System.Text.Json.Serialization;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormSubmissionListItemBackofficeModel
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? PageUrl { get; set; }
        public List<WorkflowStageStatusModel> WorkflowStages { get; set; } = [];

        // Set for a submission in the recycle bin
        public DateTime? TrashedAt { get; set; }
        public string? TrashedByName { get; set; }
    }

    public class WorkflowStageStatusModel
    {
        public string WorkflowAlias { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public int Order { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WorkflowExecutionStatus Status { get; set; }
    }
}
