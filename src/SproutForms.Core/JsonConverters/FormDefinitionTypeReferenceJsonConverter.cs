using SproutForms.Core.Models.FormTypes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SproutForms.Core.JsonConverters
{
    public class FormDefinitionTypeReferenceJsonConverter : JsonConverter<FormDefinitionTypeReference>
    {
        private readonly IReadOnlyDictionary<string, Type> _settingsTypes;

        public FormDefinitionTypeReferenceJsonConverter(IEnumerable<IFormDefinitionType> formTypes)
        {
            _settingsTypes = formTypes.ToDictionary(t => t.Alias, t => t.SettingsType);
        }

        public override FormDefinitionTypeReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var alias = root.GetProperty("TypeAlias").GetString()!;

            object settings = null!;
            if (root.TryGetProperty("Settings", out var settingsElement) && settingsElement.ValueKind != JsonValueKind.Null)
            {
                if (_settingsTypes.TryGetValue(alias, out var settingsType))
                {
                    settings = JsonSerializer.Deserialize(settingsElement.GetRawText(), settingsType, options)!;
                }
                else
                {
                    // preserve as JsonElement so we don't lose the raw data
                    settings = JsonSerializer.Deserialize<JsonElement>(settingsElement.GetRawText(), options);
                }
            }

            return new FormDefinitionTypeReference
            {
                TypeAlias = alias,
                Settings = settings
            };
        }

        public override void Write(Utf8JsonWriter writer, FormDefinitionTypeReference value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("TypeAlias", value.TypeAlias);

            writer.WritePropertyName("Settings");
            if (value.Settings is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value.Settings, value.Settings.GetType(), options);
            }

            writer.WriteEndObject();
        }
    }
}
