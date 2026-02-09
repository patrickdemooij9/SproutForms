using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Umbraco.Core.Implementations;
using Umbraco.Cms.Core.DependencyInjection;

namespace SproutForms.Umbraco.Core.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static void EnableSproutFormsRecaptchaV3(this IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IFormSubmissionGuard, RecaptchaV3SubmissionGuard>();
            builder.Services.Configure<RecaptchaV3Options>(builder.Config.GetSection("SproutForms:RecaptchaV3"));
        }
    }
}
