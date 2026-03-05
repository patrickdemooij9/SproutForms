using SproutForms.Core.Models.Flows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SproutForms.Core.JsonConverters
{
    public class FormWorkflowJsonConverter : JsonConverter<FormWorkflow>
    {
        private readonly IReadOnlyDictionary<string, Type> _configTypes;

        public FormWorkflowJsonConverter(IEnumerable<IFormWorkflowType> workflowTypes)
        {
            _configTypes = workflowTypes.ToDictionary(w => w.Alias, w => w.ConfigurationType);
        }

        public override FormWorkflow Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var alias = root.GetProperty("Alias").GetString()!;
            var workflowTypeAlias = root.GetProperty("WorkflowTypeAlias").GetString()!;
            var order = root.TryGetProperty("Order", out var orderEl) ? orderEl.GetInt32() : 0;
            var templateId = root.TryGetProperty("TemplateId", out var templateIdEl) ? templateIdEl.GetGuid() : (Guid?)null; 

            object configuration = null!;
            if (root.TryGetProperty("Configuration", out var conf) && conf.ValueKind != JsonValueKind.Null)
            {
                if (_configTypes.TryGetValue(workflowTypeAlias, out var configType))
                {
                    configuration = JsonSerializer.Deserialize(conf.GetRawText(), configType, options)!;
                }
                else
                {
                    configuration = JsonSerializer.Deserialize<JsonElement>(conf.GetRawText(), options);
                }
            }

            return new FormWorkflow
            {
                Alias = alias,
                WorkflowTypeAlias = workflowTypeAlias,
                Configuration = configuration!,
                Order = order,
                TemplateId = templateId
            };
        }

        public override void Write(Utf8JsonWriter writer, FormWorkflow value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("Alias", value.Alias);
            writer.WriteString("WorkflowTypeAlias", value.WorkflowTypeAlias);
            writer.WriteNumber("Order", value.Order);
            if (value.TemplateId.HasValue)
            {
                writer.WriteString("TemplateId", value.TemplateId?.ToString());
            }

            writer.WritePropertyName("Configuration");
            if (value.Configuration is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value.Configuration, value.Configuration.GetType(), options);
            }

            writer.WriteEndObject();
        }
    }
}
