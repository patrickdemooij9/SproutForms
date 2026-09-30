using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace SproutForms.Umbraco.Core.Headless
{
    /// <summary>
    /// Marks the headless submit endpoint, which reads its body itself because it takes both JSON and multipart/form-data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HeadlessSubmitRequestBodyAttribute : Attribute
    {
    }

    /// <summary>
    /// Documents the body of the endpoints marked with <see cref="HeadlessSubmitRequestBodyAttribute"/> in the OpenAPI document.
    /// </summary>
    public class HeadlessSubmitRequestBodyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo.GetCustomAttribute<HeadlessSubmitRequestBodyAttribute>() is null)
                return;

            var jsonSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "The values keyed by field alias, a repeater's as a list of entry objects. Texts, numbers and booleans are all taken as text. "
                    + "\"sf_PageUrl\" holds the page the form was on, and the submission guard's values sit next to the fields, such as \"g-recaptcha-response\" or the honeypot's field.",
                AdditionalProperties = new OpenApiSchema()
            };
            var multipartSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "The values as a JSON part, and each upload as a part named by its field's path, such as \"cv\" or \"people[0].cv\".",
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["values"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The values object, as JSON." }
                },
                AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" }
            };

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType { Schema = jsonSchema },
                    ["multipart/form-data"] = new OpenApiMediaType { Schema = multipartSchema }
                }
            };
        }
    }
}
