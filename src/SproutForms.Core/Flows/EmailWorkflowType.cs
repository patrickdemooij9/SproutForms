using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Flows.Email;
using SproutForms.Core.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Flows
{
    public class EmailWorkflowType : IFormWorkflowType
    {
        private readonly IEmailSender _emailSender;
        private readonly FormValueFormatter _formatter;
        private readonly WorkflowMessageResolver _messageResolver;

        public string Alias => "email";

        public Type ConfigurationType => typeof(EmailWorkflowConfig);

        public EmailWorkflowType(IEmailSender emailSender, FormValueFormatter formatter, WorkflowMessageResolver messageResolver)
        {
            _emailSender = emailSender;
            _formatter = formatter;
            _messageResolver = messageResolver;
        }

        public async Task<WorkflowExecutionResult> ExecuteAsync(WorkflowContext context, CancellationToken ct)
        {
            var config = (EmailWorkflowConfig)context.Workflow.Configuration;

            var body = BuildBody(context.Submission, context.Version);
            // A subject is one header line, and a field's value can hold line breaks
            var subject = string.Join(' ', (_messageResolver.ResolveTokens(config.Subject, context.Submission, context.Version) ?? string.Empty)
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

            try
            {
                await _emailSender.SendAsync(
                config.From,
                config.To,
                subject,
                body,
                ct);
            }
            catch (Exception ex)
            {
                return WorkflowFailures.FromEmailException(ex);
            }

            return new WorkflowExecutionResult(true);
        }

        private string BuildBody(FormSubmission submission, FormVersion version)
        {
            var sb = new StringBuilder();

            sb.AppendLine("New form submission:");
            sb.AppendLine("<br/>");

            // A field group's entries take more than one line
            foreach (var (label, value) in _formatter.FormatAll(submission.Values, version.Definition.Fields))
            {
                var html = WebUtility.HtmlEncode(value).Replace("\n", "<br/>");
                sb.AppendLine($"{WebUtility.HtmlEncode(label)}: {html}");
                sb.AppendLine("<br/>");
            }

            foreach (var variable in version.Definition.Variables.Where(it => submission.Variables.ContainsKey(it.Alias)))
            {
                var value = VariableTokens.Format(variable.Alias, submission, version.Definition);
                sb.AppendLine($"{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(variable.Label) ? variable.Alias : variable.Label)}: {WebUtility.HtmlEncode(value)}");
                sb.AppendLine("<br/>");
            }

            sb.AppendLine("<small>Mail provided by Sprout Forms</small>");

            return sb.ToString();
        }

        public object GetDefaultConfiguration()
        {
            return new EmailWorkflowConfig();
        }
    }
}
