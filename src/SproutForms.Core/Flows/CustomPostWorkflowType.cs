using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Core.Flows
{
    public class CustomPostWorkflowType : IFormWorkflowType
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string Alias => "customPost";

        public Type ConfigurationType => typeof(CustomPostWorkflowConfig);

        public CustomPostWorkflowType(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<WorkflowExecutionResult> ExecuteAsync(WorkflowContext context, CancellationToken ct)
        {
            var config = (CustomPostWorkflowConfig)context.Workflow.Configuration;

            if (string.IsNullOrEmpty(config.Url))
                return new WorkflowExecutionResult(false, "URL is required");

            var jsonBody = BuildJsonBody(context.Submission);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, config.Url)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    return new WorkflowExecutionResult(false, $"HTTP error: {response.StatusCode} - {errorBody}");
                }
            }
            catch (Exception ex)
            {
                return new WorkflowExecutionResult(false, ex.Message);
            }

            return new WorkflowExecutionResult(true);
        }

        private static string BuildJsonBody(FormSubmission submission)
        {
            var dict = new Dictionary<string, object>
            {
                ["id"] = submission.Id,
                ["formVersionId"] = submission.FormVersionId,
                ["submittedAt"] = submission.SubmittedAt,
                ["ipAddress"] = submission.IpAddress ?? string.Empty,
                ["pageUrl"] = submission.PageUrl ?? string.Empty,
                ["values"] = submission.Values
            };

            return JsonSerializer.Serialize(dict);
        }

        public object GetDefaultConfiguration()
        {
            return new CustomPostWorkflowConfig();
        }
    }
}
