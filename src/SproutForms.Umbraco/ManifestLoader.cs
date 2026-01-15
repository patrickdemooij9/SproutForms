using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SproutForms.Umbraco
{
    internal class ManifestLoader : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IPackageManifestReader, ManifestFilter>();
        }
    }

    internal class ManifestFilter : IPackageManifestReader
    {
        public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        {

            var entrypoint = JsonNode.Parse(@"{""name"": ""SproutForms.entrypoint"",
            ""alias"": ""SproutForms.EntryPoint"",
            ""type"": ""backofficeEntryPoint"",
            ""js"": ""/App_Plugins/SproutForms/index.js""}");

            List<PackageManifest> manifest = [
                new PackageManifest
            {
                Id = "SproutForms",
                Name = "SproutForms",
                AllowTelemetry = true,
                Version = "1.0.0",
                Extensions = [ entrypoint!],
            }
            ];

            return Task.FromResult(manifest.AsEnumerable());
        }
    }
}
