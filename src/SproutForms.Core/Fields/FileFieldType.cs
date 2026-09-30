using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Services;
using System.Text.Json;

namespace SproutForms.Core.Fields
{
    public class FileFieldType : FormFieldBase<FileFieldConfig, StoredFileReference>
    {
        public override string Alias => "file";

        public override FileFieldConfig DefaultConfiguration => new();

        protected override object? GetClientConfiguration(FileFieldConfig configuration)
            => new FileFieldClientConfig
            {
                MaxFileSizeBytes = configuration.MaxFileSizeBytes,
                AllowedExtensions = configuration.AllowedExtensions
            };

        // The stored value is the file's reference, as JSON text; the visitor knows the file by its name
        public override string? GetDisplayValue(JsonElement value, object configuration, FormValueFormatter formatter)
        {
            if (value.ValueKind != JsonValueKind.String)
                return FormValueFormatter.FormatText(value);

            try
            {
                return JsonSerializer.Deserialize<StoredFileReference>(value.GetString()!)?.FileName;
            }
            catch (JsonException)
            {
                return FormValueFormatter.FormatText(value);
            }
        }

        protected override ValidationResult Validate(StoredFileReference value, FileFieldConfig configuration)
        {
            return ValidationResult.Success();
        }
    }
}
