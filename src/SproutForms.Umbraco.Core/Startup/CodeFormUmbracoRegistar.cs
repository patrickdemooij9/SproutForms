using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Registry;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace SproutForms.Umbraco.Core.Startup
{
    public class CodeFormUmbracoRegistar : IAsyncComponent
    {
        private readonly IRuntimeState _runtimeState;
        private readonly IServiceProvider _services;
        private readonly CodeFormRegistry _registry;

        public CodeFormUmbracoRegistar(
            IRuntimeState runtimeState,
            IServiceProvider services,
            CodeFormRegistry registry)
        {
            _runtimeState = runtimeState;
            _services = services;
            _registry = registry;
        }

        public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
        {
            if (_runtimeState.Level != RuntimeLevel.Run) return;

            using var scope = _services.CreateScope();
            var formsRepo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
            var versionsRepo = scope.ServiceProvider.GetRequiredService<IFormVersionRepository>();
            var typeValidator = scope.ServiceProvider.GetRequiredService<FormDefinitionTypeValidator>();
            var auditRepo = scope.ServiceProvider.GetRequiredService<IFormAuditRepository>();

            foreach (var factory in _registry.Factories)
            {
                var form = factory(scope.ServiceProvider);
                await RegisterAsync(form, formsRepo, versionsRepo, auditRepo, typeValidator);
            }
        }

        public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task RegisterAsync(
            ICodeFirstForm codeForm,
            IFormRepository formsRepo,
            IFormVersionRepository versionsRepo,
            IFormAuditRepository auditRepo,
            FormDefinitionTypeValidator typeValidator)
        {
            var definition = codeForm.Build();
            var errors = FormDefinitionStructureValidator.Validate(definition).Concat(typeValidator.Validate(definition)).ToList();
            if (errors.Count > 0)
                throw new InvalidOperationException($"Code-first form '{codeForm.Alias}' is invalid: {string.Join(" ", errors)}");

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
                auditRepo.Add(CreateAuditEntry(form.Id, FormAuditAction.Created, version));

                return;
            }

            var latest = versionsRepo.GetLatest(form.Id);
            if (latest!.DefinitionHash == hash)
                return;

            // A new instance, because the latest version is the repository's cached one
            var newVersion = new FormVersion
            {
                Id = Guid.NewGuid(),
                FormId = form.Id,
                Version = latest.Version + 1,
                Status = FormStatus.Published,
                Definition = definition,
                DefinitionHash = hash,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            versionsRepo.Add(newVersion);
            auditRepo.Add(CreateAuditEntry(form.Id, FormAuditAction.Saved, newVersion));
        }

        private static FormAuditEntry CreateAuditEntry(Guid formId, FormAuditAction action, FormVersion version)
        {
            return new FormAuditEntry
            {
                FormId = formId,
                Action = action,
                UserKey = version.CreatedBy,
                CreatedAt = version.CreatedAt,
                VersionId = version.Id
            };
        }
    }
}
