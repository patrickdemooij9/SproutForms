using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SproutForms.Core.JsonConverters
{
    public class FormSubmitOutcomeJsonConverter : JsonConverter<FormSubmitOutcome>
    {
        private readonly IReadOnlyDictionary<string, Type> _configTypes;

        public FormSubmitOutcomeJsonConverter(IEnumerable<IFormSubmitOutcomeType> outcomeTypes)
        {
            _configTypes = outcomeTypes.ToDictionary(o => o.Alias, o => o.ConfigurationType);
        }

        public override FormSubmitOutcome Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var alias = root.GetProperty("OutcomeTypeAlias").GetString()!;

            object configuration = null!;
            if (root.TryGetProperty("Configuration", out var conf) && conf.ValueKind != JsonValueKind.Null)
            {
                if (_configTypes.TryGetValue(alias, out var configType))
                {
                    configuration = JsonSerializer.Deserialize(conf.GetRawText(), configType, options)!;
                }
                else
                {
                    // preserve as JsonElement so we don't lose the raw data
                    configuration = JsonSerializer.Deserialize<JsonElement>(conf.GetRawText(), options);
                }
            }

            return new FormSubmitOutcome
            {
                OutcomeTypeAlias = alias,
                Configuration = configuration!
            };
        }

        public override void Write(Utf8JsonWriter writer, FormSubmitOutcome value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("OutcomeTypeAlias", value.OutcomeTypeAlias);

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
