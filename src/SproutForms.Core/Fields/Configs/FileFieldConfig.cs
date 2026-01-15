using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Fields.Configs
{
    public class FileFieldConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.Integer")]
        public long MaxFileSizeBytes { get; set; } = 10_000_000;

        [BackofficeField("Umb.PropertyEditorUi.MultipleTextstring")]
        public IReadOnlyList<string>? AllowedExtensions { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string StorageProviderAlias { get; set; } = "default";
    }
}
