using Microsoft.OpenApi;
using SproutForms.Umbraco.Core.Models.Headless;
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

            var jsonSchema = context.SchemaGenerator.GenerateSchema(typeof(HeadlessSubmitRequest), context.SchemaRepository);
            var multipartSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "values and guard as JSON, pageUrl as text, and each upload as a part named after its field.",
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["values"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The values object of HeadlessSubmitRequest, as JSON." },
                    ["pageUrl"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["guard"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The guard object of HeadlessSubmitRequest, as JSON." }
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
