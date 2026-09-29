namespace SproutForms.Core.Fields.Configs
{
    /// <summary>
    /// What a front-end sees of a <see cref="FileFieldConfig"/>: the limits to check before uploading, without the storage provider.
    /// </summary>
    public class FileFieldClientConfig
    {
        public long MaxFileSizeBytes { get; init; }
        public IReadOnlyList<string>? AllowedExtensions { get; init; }
    }
}
