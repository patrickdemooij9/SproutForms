using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SproutForms.Core.Fields;
using SproutForms.Core.Models;
using SproutForms.Core.Flows;
using SproutForms.Core.Hosting;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Flows.Email;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Core.Registry;
using SproutForms.Core.Rendering;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Core.Storage;
using SproutForms.Core.Storage.InMemory;

namespace SproutForms.Core
{
    public static class SproutFormsServiceCollectionExtensions
    {
        /// <summary>
        /// Registers what every host needs: the services, the built-in field, outcome and workflow types, rendering and the honeypot guard.
        /// It doesn't register storage, an <see cref="IEmailSender"/> or anything that runs on a schedule: the host adds those, like
        /// <see cref="AddSproutFormsStandalone"/> and <see cref="AddSproutFormsInMemoryStorage"/> do for a site without Umbraco.
        /// </summary>
        public static IServiceCollection AddSproutForms(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SproutFormsOptions>(configuration.GetSection("SproutForms"));
            services.Configure<LocalDiskFileStorageOptions>(configuration.GetSection("SproutForms:LocalDiskFileStorage"));
            services.AddHttpClient();
            services.AddHttpContextAccessor();

            services.AddSingleton<FormClientModelBuilder>();
            services.AddSingleton<FormSubmitOutcomeRunner>();
            services.AddSingleton<FormCalculator>();
            services.AddTransient<FormRenderingService>();
            services.AddSingleton<FormThemeViewResolver>();
            services.AddSingleton<IFormSubmissionService, FormSubmissionService>();
            services.AddSingleton<FormDeletionService>();
            services.AddSingleton<FormRecycleBinService>();
            services.AddSingleton<FormSubmissionRecycleBinService>();
            services.AddSingleton<RecycleBinCleanupService>();
            services.AddSingleton<IConditionEvaluator, ConditionEvaluator>();
            services.AddSingleton<FormValueFormatter>();
            services.AddSingleton<WorkflowMessageResolver>();
            services.AddSingleton<WorkflowTemplateService>();
            services.AddSingleton<CodeFormRegistrar>();

            services.AddSingleton<IFormDefinitionType, StandardFormDefinitionType>();
            services.AddSingleton<FormDefinitionTypeValidator>();

            services.AddSingleton<IFormFieldType, TextFieldFormFieldType>();
            services.AddSingleton<IFormFieldType, EmailFieldType>();
            services.AddSingleton<IFormFieldType, TextAreaFieldType>();
            services.AddSingleton<IFormFieldType, CheckboxFieldType>();
            services.AddSingleton<IFormFieldType, SelectFieldType>();
            services.AddSingleton<IFormFieldType, HiddenFieldType>();
            services.AddSingleton<IFormFieldType, RadioFieldType>();
            services.AddSingleton<IFormFieldType, DateFieldType>();
            services.AddSingleton<IFormFieldType, FileFieldType>();
            services.AddSingleton<IFormFieldType, RepeaterFieldType>();

            services.AddSingleton<IFormFileStorageProvider, LocalDiskFileStorageProvider>();

            services.AddSingleton<IFormSubmitOutcomeType, ShowMessageOutcome>();
            services.AddSingleton<IFormSubmitOutcomeType, RedirectUrlOutcomeType>();

            services.AddSingleton<IFormWorkflowType, EmailWorkflowType>();
            services.AddSingleton<IFormWorkflowType, SlackWorkflowType>();
            services.AddSingleton<IFormWorkflowType, TeamsWorkflowType>();
            services.AddSingleton<IFormWorkflowType, CustomPostWorkflowType>();
            services.AddSingleton<IWorkflowRunner, WorkflowRunner>();
            services.AddSingleton<PendingWorkflowProcessor>();

            services.AddSingleton<IFormSubmissionGuard, HoneypotSubmissionGuard>();

            services.AddCodeFirstForms(_ => { });

            return services;
        }

        /// <summary>
        /// Sets SproutForms up on an ASP.NET Core site without Umbraco: <see cref="AddSproutForms"/>, plus registering the code-first forms
        /// at startup, running the workflows and emptying the recycle bin in the background, and sending emails with SMTP (SproutForms:Smtp).
        /// Add storage too, such as <see cref="AddSproutFormsInMemoryStorage"/>.
        /// </summary>
        public static IServiceCollection AddSproutFormsStandalone(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSproutForms(configuration);

            services.Configure<SmtpEmailOptions>(configuration.GetSection("SproutForms:Smtp"));
            services.TryAddSingleton<IEmailSender, SmtpEmailSender>();

            services.AddHostedService<CodeFormRegistrarHostedService>();
            services.AddHostedService<WorkflowExecutionBackgroundService>();
            services.AddHostedService<RecycleBinCleanupBackgroundService>();

            return services;
        }

        /// <summary>
        /// Keeps forms, submissions and workflow executions in memory. Everything is gone when the site restarts, and every server of a
        /// load balanced site has its own, so it suits code-first forms whose submissions are handled by their workflows, and trying SproutForms out.
        /// </summary>
        public static IServiceCollection AddSproutFormsInMemoryStorage(this IServiceCollection services)
        {
            services.AddSingleton<InMemoryStore>();
            services.AddSingleton<IFormRepository, InMemoryFormRepository>();
            services.AddSingleton<IFormVersionRepository, InMemoryFormVersionRepository>();
            services.AddSingleton<IFormSubmissionRepository, InMemoryFormSubmissionRepository>();
            services.AddSingleton<IFolderRepository, InMemoryFolderRepository>();
            services.AddSingleton<IFormAuditRepository, InMemoryFormAuditRepository>();
            services.AddSingleton<IWorkflowExecutionRepository, InMemoryWorkflowExecutionRepository>();
            services.AddSingleton<IWorkflowTemplateRepository, InMemoryWorkflowTemplateRepository>();
            services.AddSingleton<IUnitOfWorkProvider, InMemoryUnitOfWorkProvider>();

            return services;
        }
    }
}
