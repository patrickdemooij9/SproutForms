using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Registry
{
    public class CodeFormRegistrar : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly CodeFormRegistry _registry;

        public CodeFormRegistrar(
            IServiceProvider services,
            CodeFormRegistry registry)
        {
            _services = services;
            _registry = registry;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var formsRepo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
            var versionsRepo = scope.ServiceProvider.GetRequiredService<IFormVersionRepository>();

            foreach (var factory in _registry.Factories)
            {
                var form = factory(scope.ServiceProvider);
                await RegisterAsync(form, formsRepo, versionsRepo);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private async Task RegisterAsync(
            ICodeFirstForm codeForm,
            IFormRepository formsRepo,
            IFormVersionRepository versionsRepo)
        {
            var definition = codeForm.Build();
            var hash = FormDefinitionHasher.Hash(definition);

            var form = formsRepo.GetByAlias(codeForm.Alias);
            if (form is null)
            {
                form = new Form
                {
                    Id = Guid.NewGuid(),
                    Name = codeForm.Alias,
                    Alias = codeForm.Alias,
                    Source = FormSource.Code
                };

                formsRepo.Save(form);

                var version = new FormVersion
                {
                    Id = Guid.NewGuid(),
                    FormId = form.Id,
                    Version = 1,
                    Status = FormStatus.Published,
                    Definition = definition,
                    DefinitionHash = hash,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                versionsRepo.Add(version);

                return;
            }

            var latest = versionsRepo.GetLatest(form.Id);
            if (latest!.DefinitionHash == hash)
                return;

            latest.Id = Guid.NewGuid();
            latest.Version++;
            latest.Definition = definition;
            latest.DefinitionHash = hash;
            versionsRepo.Add(latest);
        }
    }
}
