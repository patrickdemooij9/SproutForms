using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;

namespace SproutForms.Core.Registry
{
    /// <summary>
    /// Stores the code-first forms of the <see cref="CodeFormRegistry"/>: a new form, or a new published version when its definition changed.
    /// The host runs it once at startup, when its repositories are ready.
    /// </summary>
    public class CodeFormRegistrar
    {
        private readonly IServiceProvider _services;
        private readonly CodeFormRegistry _registry;

        public CodeFormRegistrar(IServiceProvider services, CodeFormRegistry registry)
        {
            _services = services;
            _registry = registry;
        }

        public void RegisterAll()
        {
            using var scope = _services.CreateScope();
            var formsRepo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
            var versionsRepo = scope.ServiceProvider.GetRequiredService<IFormVersionRepository>();
            var typeValidator = scope.ServiceProvider.GetRequiredService<FormDefinitionTypeValidator>();
            var auditRepo = scope.ServiceProvider.GetRequiredService<IFormAuditRepository>();

            foreach (var factory in _registry.Factories)
            {
                var form = factory(scope.ServiceProvider);
                Register(form, formsRepo, versionsRepo, auditRepo, typeValidator);
            }
        }

        private static void Register(
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
                    CreatedBy = SystemUser.Key
                };

                versionsRepo.Add(version);
                auditRepo.Add(CreateAuditEntry(form.Id, FormAuditAction.Created, version));

                return;
            }

            var latest = versionsRepo.GetLatest(form.Id);
            if (latest!.DefinitionHash == hash)
                return;

            // A new instance, because the latest version may be the repository's cached one
            var newVersion = new FormVersion
            {
                Id = Guid.NewGuid(),
                FormId = form.Id,
                Version = latest.Version + 1,
                Status = FormStatus.Published,
                Definition = definition,
                DefinitionHash = hash,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = SystemUser.Key
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
