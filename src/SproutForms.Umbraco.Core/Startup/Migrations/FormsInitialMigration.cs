using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    internal class FormsInitialMigration : AsyncMigrationBase
    {
        public FormsInitialMigration(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            if (!TableExists("SproutForms_Forms"))
            {
                Create.Table<FormEntity>().Do();
            }
            if (!TableExists("SproutForms_FormVersions"))
            {
                Create.Table<FormVersionEntity>().Do();
            }
            if (!TableExists("SproutForms_FormSubmissions"))
            {
                Create.Table<FormSubmissionEntity>().Do();
            }
            if (!TableExists("SproutForms_WorkflowExecutions"))
            {
                Create.Table<WorkflowExecutionEntity>().Do();
            }
            return Task.CompletedTask;
        }
    }
}
