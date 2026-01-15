namespace SproutForms.Core.Models.Flows
{
    public sealed record WorkflowExecutionResult(
    bool Success,
    string? Error = null,
    bool Retryable = false);
}
