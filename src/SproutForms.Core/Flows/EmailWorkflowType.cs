using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Flows.Email;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Flows
{
    public class EmailWorkflowType : IFormWorkflowType
    {
        private readonly IEmailSender _emailSender;

        public string Alias => "email";

        public string DisplayName => "Send email";

        public Type ConfigurationType => typeof(EmailWorkflowConfig);

        public EmailWorkflowType(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task<WorkflowExecutionResult> ExecuteAsync(FormWorkflow workflow, FormSubmission submission, CancellationToken ct)
        {
            var config = (EmailWorkflowConfig) workflow.Configuration;

            var body = BuildBody(submission);

            await _emailSender.SendAsync(
                config.From,
                config.To,
                config.Subject,
                body,
                ct);

            return new WorkflowExecutionResult(true);
        }

        private static string BuildBody(FormSubmission submission)
        {
            var sb = new StringBuilder();

            sb.AppendLine("New form submission:");
            sb.AppendLine();

            foreach (var field in submission.Values)
            {
                sb.AppendLine($"{field.Key}: {field.Value}");
            }

            return sb.ToString();
        }

        public object GetDefaultConfiguration()
        {
            return new EmailWorkflowConfig();
        }
    }
}
