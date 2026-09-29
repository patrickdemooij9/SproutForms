using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SproutForms.Core;
using System.Security.Cryptography;
using System.Text;

namespace SproutForms.Umbraco.Core.Headless
{
    /// <summary>
    /// Makes the headless endpoints answer 404 while SproutForms:Headless:Enabled is off, and requires the Api-Key header when an API key is configured.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HeadlessApiAccessAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<SproutFormsOptions>>().CurrentValue.Headless;
            if (!options.Enabled)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (!string.IsNullOrEmpty(options.ApiKey) && !IsApiKey(context.HttpContext.Request.Headers[HeadlessApi.ApiKeyHeaderName].ToString(), options.ApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = 401,
                    Title = $"A valid {HeadlessApi.ApiKeyHeaderName} header is required."
                });
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        private static bool IsApiKey(string value, string apiKey)
            => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(value), Encoding.UTF8.GetBytes(apiKey));
    }
}
