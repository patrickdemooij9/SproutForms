using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SproutForms.Core.JsonConverters
{
    public class FormFieldJsonConverter : JsonConverter<FormField>
    {
        private readonly IReadOnlyDictionary<Guid, Type> _configTypes;

        public FormFieldJsonConverter(IEnumerable<IFormFieldType> fieldTypes)
        {
            _configTypes = fieldTypes.ToDictionary(ft => ft.Id, ft => ft.ConfigurationType);
        }

        public override FormField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var alias = root.GetProperty("Alias").GetString()!;
            var label = root.GetProperty("Label").GetString()!;
            var fieldTypeId = Guid.Parse(root.GetProperty("FieldTypeId").GetString()!);
            var required = root.TryGetProperty("Required", out var req) && req.GetBoolean();

            FieldConditions? conditions = null;
            if (root.TryGetProperty("Conditions", out var cond) && cond.ValueKind != JsonValueKind.Null)
            {
                conditions = JsonSerializer.Deserialize<FieldConditions>(cond.GetRawText(), options);
            }

            object configuration = null!;
            if (root.TryGetProperty("Configuration", out var conf) && conf.ValueKind != JsonValueKind.Null)
            {
                if (_configTypes.TryGetValue(fieldTypeId, out var configType))
                {
                    configuration = JsonSerializer.Deserialize(conf.GetRawText(), configType, options)!;
                }
                else
                {
                    configuration = JsonSerializer.Deserialize<object>(conf.GetRawText(), options)!;
                }
            }

            return new FormField
            {
                Alias = alias,
                Label = label,
                FieldTypeId = fieldTypeId,
                Required = required,
                Configuration = configuration,
                Conditions = conditions
            };
        }

        public override void Write(Utf8JsonWriter writer, FormField value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("Alias", value.Alias);
            writer.WriteString("Label", value.Label);
            writer.WriteString("FieldTypeId", value.FieldTypeId.ToString());
            writer.WriteBoolean("Required", value.Required);

            writer.WritePropertyName("Configuration");
            if (value.Configuration is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value.Configuration, value.Configuration.GetType(), options);
            }

            writer.WritePropertyName("Conditions");
            if (value.Conditions is null)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, value.Conditions, options);

            writer.WriteEndObject();
        }
    }
}
