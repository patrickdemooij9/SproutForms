using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;

namespace SproutForms.Core.Flows
{
    public class SlackWorkflowType : IFormWorkflowType
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string Alias => "slack";

        public Type ConfigurationType => typeof(SlackWorkflowConfig);

        public SlackWorkflowType(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<WorkflowExecutionResult> ExecuteAsync(WorkflowContext context, CancellationToken ct)
        {
            var config = (SlackWorkflowConfig)context.Workflow.Configuration;

            if (string.IsNullOrEmpty(config.WebhookUrl))
                return new WorkflowExecutionResult(false, "Slack webhook URL is required");

            var resolvedMessage = WorkflowMessageResolver.ResolveTokens(config.Message, context.Submission);

            var payload = BuildSlackPayload(resolvedMessage, context.Submission, context.Version);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(config.WebhookUrl, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    return new WorkflowExecutionResult(false, $"Slack API error: {response.StatusCode} - {errorBody}");
                }
            }
            catch (Exception ex)
            {
                return new WorkflowExecutionResult(false, ex.Message);
            }

            return new WorkflowExecutionResult(true);
        }

        private object BuildSlackPayload(string message, FormSubmission submission, FormVersion version)
        {
            var fields = submission.Values
                .Where(v => !string.IsNullOrEmpty(v.Value.GetString()))
                .Select(v => new
                {
                    type = "mrkdwn",
                    text = $"*{version.Definition.Fields.First(it => it.Alias == v.Key).Label}:*\n{v.Value.GetString()}"
                })
                .ToList();
            var blocks = new List<object>
            {
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = "New submission:\r\n" + message + "\r\n" + string.Join("\r\n", fields.Select(it => it.text))
                    }
                }
            };

            if (fields.Count > 0)
            {
                blocks.Add(new
                {
                    type = "section",
                    fields = fields.Take(10).ToList()
                });
            }

            return new { blocks };
        }

        public object GetDefaultConfiguration()
        {
            return new SlackWorkflowConfig();
        }
    }
}
