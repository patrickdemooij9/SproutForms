namespace SproutForms.Core.Models.Flows
{
    public class WorkflowContext
    {
        public required FormWorkflow Workflow { get; set; }
        public required FormSubmission Submission { get; set; }
        public required FormVersion Version { get; set; }
    }
}
