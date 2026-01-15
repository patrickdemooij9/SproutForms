namespace SproutForms.Core.Models.Files
{
    public sealed record StoredFileReference(
        Guid Id,
        string FileName,
        long Size,
        string ContentType,
        string StorageProvider
        );
}
