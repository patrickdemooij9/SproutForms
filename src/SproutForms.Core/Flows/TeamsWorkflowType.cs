using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Services;

namespace SproutForms.Core.Flows
{
    public class TeamsWorkflowType : IFormWorkflowType
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string Alias => "teams";

        public Type ConfigurationType => typeof(TeamsWorkflowConfig);

        public TeamsWorkflowType(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<WorkflowExecutionResult> ExecuteAsync(WorkflowContext context, CancellationToken ct)
        {
            var config = (TeamsWorkflowConfig)context.Workflow.Configuration;

            if (string.IsNullOrEmpty(config.WebhookUrl))
                return new WorkflowExecutionResult(false, "Teams webhook URL is required");

            var resolvedMessage = WorkflowMessageResolver.ResolveTokens(config.Message, context.Submission);

            var payload = BuildTeamsPayload(resolvedMessage, config.ThemeColor, context.Submission);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(config.WebhookUrl, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    return new WorkflowExecutionResult(false, $"Teams API error: {response.StatusCode} - {errorBody}");
                }
            }
            catch (Exception ex)
            {
                return new WorkflowExecutionResult(false, ex.Message);
            }

            return new WorkflowExecutionResult(true);
        }

        private static object BuildTeamsPayload(string message, string themeColor, FormSubmission submission)
        {
            var sections = new List<object>
            {
                new
                {
                    text = message
                }
            };

            var facts = submission.Values
                .Where(v => !string.IsNullOrEmpty(v.Value.GetString()))
                .Select(v => new
                {
                    name = v.Key,
                    value = v.Value.GetString()
                })
                .ToList();

            if (facts.Count > 0)
            {
                sections.Add(new
                {
                    facts = facts
                });
            }

            var payload = new
            {
                themeColor = string.IsNullOrEmpty(themeColor) ? "0078D4" : themeColor,
                sections = sections
            };

            return payload;
        }

        public object GetDefaultConfiguration()
        {
            return new TeamsWorkflowConfig();
        }
    }
}
