using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Flows.Models;
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

            var resolvedMessage = WorkflowMessageResolver.ResolveTokens(config.Message, context.Submission, context.Version);

            var payload = BuildTeamsPayload(resolvedMessage, context.Submission);

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

        private static TeamsMessageModel BuildTeamsPayload(string message, FormSubmission submission)
        {
            var payload = new TeamsMessageModel
            {
                Attachments =
                [
                    new TeamsAttachmentModel
                    {
                        Content = new TeamsAdaptiveCardModel
                        {
                            Body =
                            [
                                new TeamsTextBlockModel
                                {
                                    Text = message
                                }
                            ]
                        }
                    }
                ]
            };

            return payload;
        }

        public object GetDefaultConfiguration()
        {
            return new TeamsWorkflowConfig
            {
                Message = "A new submission has been submitted:\r\n#AllValues"
            };
        }
    }
}
