using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.FormTypes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SproutForms.Core.JsonConverters
{
    public class FormFieldJsonConverter : JsonConverter<FormField>
    {
        private readonly IReadOnlyDictionary<string, Type> _configTypes;
        private readonly IReadOnlyDictionary<(string FormTypeAlias, string FieldTypeAlias), Type> _extensionSettingsTypes;

        public FormFieldJsonConverter(IEnumerable<IFormFieldType> fieldTypes, IEnumerable<IFormDefinitionType> formTypes)
        {
            _configTypes = fieldTypes.ToDictionary(ft => ft.Alias, ft => ft.ConfigurationType);
            _extensionSettingsTypes = formTypes
                .SelectMany(formType => formType.FieldExtensions.Select(extension => (formType.Alias, extension)))
                .ToDictionary(it => (it.Alias, it.extension.FieldTypeAlias), it => it.extension.SettingsType);
        }

        public override FormField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var alias = root.GetProperty("Alias").GetString()!;
            var label = root.GetProperty("Label").GetString()!;
            var fieldTypeAlias = root.GetProperty("FieldTypeAlias").GetString()!;
            var required = root.TryGetProperty("Required", out var req) && req.GetBoolean();

            var rules = new List<FieldRule>();
            if (root.TryGetProperty("Rules", out var rulesElement) && rulesElement.ValueKind == JsonValueKind.Array)
            {
                rules = JsonSerializer.Deserialize<List<FieldRule>>(rulesElement.GetRawText(), options) ?? [];
            }

            object configuration = null!;
            if (root.TryGetProperty("Configuration", out var conf) && conf.ValueKind != JsonValueKind.Null)
            {
                if (_configTypes.TryGetValue(fieldTypeAlias, out var configType))
                {
                    configuration = JsonSerializer.Deserialize(conf.GetRawText(), configType, options)!;
                }
                else
                {
                    configuration = JsonSerializer.Deserialize<object>(conf.GetRawText(), options)!;
                }
            }

            FormFieldExtensionValue? extension = null;
            if (root.TryGetProperty("Extension", out var ext) && ext.ValueKind != JsonValueKind.Null)
            {
                var formTypeAlias = ext.GetProperty("FormTypeAlias").GetString()!;
                var settings = ext.GetProperty("Settings");
                extension = new FormFieldExtensionValue
                {
                    FormTypeAlias = formTypeAlias,
                    Settings = _extensionSettingsTypes.TryGetValue((formTypeAlias, fieldTypeAlias), out var settingsType)
                        ? JsonSerializer.Deserialize(settings.GetRawText(), settingsType, options)!
                        // preserve as JsonElement so we don't lose the raw data
                        : JsonSerializer.Deserialize<JsonElement>(settings.GetRawText(), options)
                };
            }

            return new FormField
            {
                Alias = alias,
                Label = label,
                FieldTypeAlias = fieldTypeAlias,
                Required = required,
                Configuration = configuration,
                Rules = rules,
                Extension = extension
            };
        }

        public override void Write(Utf8JsonWriter writer, FormField value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("Alias", value.Alias);
            writer.WriteString("Label", value.Label);
            writer.WriteString("FieldTypeAlias", value.FieldTypeAlias.ToString());
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

            writer.WritePropertyName("Rules");
            JsonSerializer.Serialize(writer, value.Rules, options);

            writer.WritePropertyName("Extension");
            if (value.Extension is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartObject();
                writer.WriteString("FormTypeAlias", value.Extension.FormTypeAlias);
                writer.WritePropertyName("Settings");
                JsonSerializer.Serialize(writer, value.Extension.Settings, value.Extension.Settings.GetType(), options);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
    }
}
