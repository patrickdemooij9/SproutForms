using Microsoft.AspNetCore.Http;

namespace SproutForms.Core.Models.Files
{
    public interface IFormFileStorageProvider
    {
        string Alias { get; }

        Task<StoredFileReference> SaveAsync(
            IFormFile file,
            CancellationToken ct);

        Task<Stream> OpenReadAsync(
            StoredFileReference reference,
            CancellationToken ct);

        Task DeleteAsync(
            StoredFileReference reference,
            CancellationToken ct);
    }
}
