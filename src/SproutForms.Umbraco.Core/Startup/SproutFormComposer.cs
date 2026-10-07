using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core;
using SproutForms.Core.Fields;
using SproutForms.Core.Flows;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Flows.Email;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Core.Registry;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Core.Storage;
using SproutForms.Umbraco.Core.Descriptors.Fields;
using SproutForms.Umbraco.Core.Descriptors.Flows;
using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using SproutForms.Umbraco.Core.Extensions;
using SproutForms.Umbraco.Core.Headless;
using SproutForms.Umbraco.Core.Implementations;
using SproutForms.Umbraco.Core.Repositories;
using SproutForms.Umbraco.Core.Security;
using SproutForms.Umbraco.Core.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Startup
{
    public class SproutFormComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSproutForms(builder.Config);

            builder.Services.Configure<SwaggerGenOptions>(options =>
            {
                options.SwaggerDoc("sproutForms", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "Sprout Forms API",
                    Version = "Latest"
                });
            });

            builder.Services.AddSingleton<IFormSubmissionRepository, FormSubmissionRepository>();
            builder.Services.AddSingleton<IFormVersionRepository, FormVersionRepository>();
            builder.Services.AddSingleton<IFormRepository, FormRepository>();
            builder.Services.AddSingleton<IFolderRepository, FolderRepository>();
            builder.Services.AddSingleton<IFormAuditRepository, FormAuditRepository>();
            builder.Services.AddSingleton<IWorkflowExecutionRepository, WorkflowExecutionRepository>();
            builder.Services.AddSingleton<IWorkflowTemplateRepository, WorkflowTemplateRepository>();
            builder.Services.AddSingleton<IUnitOfWorkProvider, ScopeUnitOfWorkProvider>();

            builder.Services.AddSingleton<ISproutFormsDashboardService, SproutFormsDashboardService>();
            builder.Services.AddSingleton<FormVersionComparer>();
            builder.Services.AddSingleton<FormHistoryService>();
            builder.Services.AddSingleton<BackofficeUserNameResolver>();

            builder.Services.AddSingleton<IFormDefinitionTypeDescriptor, StandardFormDefinitionTypeDescriptor>();

            builder.Services.AddSingleton<IFieldDescriptor, TextFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, TextAreaFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, HiddenFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, FileFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, EmailFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, DateFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, SelectFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, RadioFieldDescriptor>();
            builder.Services.AddSingleton<IFieldDescriptor, RepeaterFieldDescriptor>();

            builder.Services.AddSingleton<IOutcomeDescriptor, ShowMessageOutcomeDescriptor>();
            builder.Services.AddSingleton<IOutcomeDescriptor, RedirectUrlOutcomeDescriptor>();
            builder.Services.AddSingleton<IOutcomeDescriptor, RedirectUmbracoPageOutcomeDescriptor>();

            builder.Services.AddSingleton<IFlowDescriptor, EmailWorkflowDescriptor>();
            builder.Services.AddSingleton<IFlowDescriptor, SlackWorkflowDescriptor>();
            builder.Services.AddSingleton<IFlowDescriptor, TeamsWorkflowDescriptor>();
            builder.Services.AddSingleton<IFlowDescriptor, CustomPostWorkflowDescriptor>();

            builder.Services.AddSingleton<IFormSubmitOutcomeType, RedirectUmbracoPageOutcomeType>();
            builder.Services.AddSingleton<IEmailSender, UmbracoEmailSender>();

            builder.Components().Append<CodeFormUmbracoRegistar>();
            builder.Services.AddRecurringBackgroundJob<Implementations.WorkflowExecutionWorker>();
            builder.Services.AddRecurringBackgroundJob<Implementations.RecycleBinCleanupJob>();

            builder.AddSproutFormsAuthorization();
            builder.AddSproutFormsHeadless();

            //builder.EnableSproutFormsRecaptchaV3();
        }
    }
}
