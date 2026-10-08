using System.Net;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Core.Flows
{
    /// <summary>
    /// Turns a failed attempt of a built-in workflow into its result. Only a failure that may pass by itself, such as a network error or
    /// a busy server, is retryable; the <see cref="WorkflowRunner"/> retries it a limited number of times.
    /// </summary>
    internal static class WorkflowFailures
    {
        public static async Task<WorkflowExecutionResult> FromResponseAsync(HttpResponseMessage response, string errorPrefix, CancellationToken ct)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return new WorkflowExecutionResult(false, $"{errorPrefix}: {response.StatusCode} - {errorBody}", IsTemporary(response.StatusCode));
        }

        // A request that didn't get an answer, or timed out, may succeed later
        public static WorkflowExecutionResult FromHttpException(Exception ex)
            => new(false, ex.Message, ex is HttpRequestException or OperationCanceledException or IOException);

        // An email that can't be sent because of its configuration or an address fails every time; anything else, such as an SMTP
        // server that can't be reached, may succeed later
        public static WorkflowExecutionResult FromEmailException(Exception ex)
            => new(false, ex.Message, ex is not (InvalidOperationException or ArgumentException or FormatException));

        private static bool IsTemporary(HttpStatusCode statusCode)
            => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
    }
}
