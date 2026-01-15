using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SproutForms.Core.Models.Files;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace SproutForms.Core.Storage
{
    public sealed class LocalDiskFileStorageProvider
    : IFormFileStorageProvider
    {
        private readonly LocalDiskFileStorageOptions _options;
        private readonly IWebHostEnvironment _env;

        public string Alias => "local";

        public LocalDiskFileStorageProvider(
            IOptions<LocalDiskFileStorageOptions> options,
            IWebHostEnvironment env)
        {
            _options = options.Value;
            _env = env;
        }

        public async Task<StoredFileReference> SaveAsync(
            IFormFile file,
            CancellationToken ct)
        {
            var id = Guid.NewGuid();
            var extension = Path.GetExtension(file.FileName);

            var fileName = $"{id}{extension}";
            var absoluteRoot = Path.Combine(_env.ContentRootPath, _options.RootPath);
            var absolutePath = Path.Combine(absoluteRoot, fileName);

            Directory.CreateDirectory(absoluteRoot);

            await using var stream = new FileStream(
                absolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await file.CopyToAsync(stream, ct);

            return new StoredFileReference(
                Id: id,
                FileName: file.FileName,
                Size: file.Length,
                ContentType: file.ContentType,
                StorageProvider: Alias
            );
        }

        public Task<Stream> OpenReadAsync(
            StoredFileReference reference,
            CancellationToken ct)
        {
            var absoluteRoot = Path.Combine(_env.ContentRootPath, _options.RootPath);
            var path = Path.Combine(absoluteRoot, $"{reference.Id}{Path.GetExtension(reference.FileName)}");

            Stream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            return Task.FromResult(stream);
        }

        public Task DeleteAsync(
            StoredFileReference reference,
            CancellationToken ct)
        {
            var absoluteRoot = Path.Combine(_env.ContentRootPath, _options.RootPath);
            var path = Path.Combine(absoluteRoot, $"{reference.Id}{Path.GetExtension(reference.FileName)}");

            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }
    }

}
