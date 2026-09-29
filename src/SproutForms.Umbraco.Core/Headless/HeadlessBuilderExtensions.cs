using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SproutForms.Core;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace SproutForms.Umbraco.Core.Headless
{
    public static class HeadlessBuilderExtensions
    {
        /// <summary>
        /// Registers the CORS policy and the OpenAPI document of the headless API. Without SproutForms:Headless:Enabled only the
        /// endpoints exist, and they answer 404.
        /// </summary>
        public static IUmbracoBuilder AddSproutFormsHeadless(this IUmbracoBuilder builder)
        {
            if (!builder.Config.GetValue<bool>("SproutForms:Headless:Enabled"))
                return builder;

            builder.Services.AddCors();
            builder.Services.AddOptions<CorsOptions>()
                .Configure<IOptions<SproutFormsOptions>>((cors, sproutForms) => cors.AddPolicy(HeadlessApi.CorsPolicyName, policy => policy
                    .WithOrigins([.. sproutForms.Value.Headless.AllowedOrigins.Select(it => it.TrimEnd('/'))])
                    .WithMethods("GET", "POST")
                    .AllowAnyHeader()
                    .WithExposedHeaders("ETag")));

            // Umbraco doesn't add the CORS middleware itself; it has to run between routing and the endpoints
            builder.Services.Configure<UmbracoPipelineOptions>(options => options.AddFilter(new UmbracoPipelineFilter("SproutForms.Headless")
            {
                PostRouting = app => app.UseCors()
            }));

            builder.Services.Configure<SwaggerGenOptions>(options =>
            {
                options.SwaggerDoc(HeadlessApi.DocumentName, new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "Sprout Forms Delivery API",
                    Version = "v1",
                    Description = "Read published forms and submit them from a headless front-end. Unstable until SproutForms 1.0."
                });
                options.OperationFilter<HeadlessSubmitRequestBodyOperationFilter>();
            });

            return builder;
        }
    }
}
