namespace SproutForms.Core.Fields.Configs
{
    public class FileFieldConfig
    {
        public long MaxFileSizeBytes { get; set; } = 10_000_000;
        public IReadOnlyList<string>? AllowedExtensions { get; set; }
        public string StorageProviderAlias { get; set; } = "default";
    }
}
