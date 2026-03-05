using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    internal class AddWorkflowTemplatesMigration : MigrationBase
    {
        public AddWorkflowTemplatesMigration(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            if (!TableExists("SproutForms_WorkflowTemplates"))
            {
                Create.Table<WorkflowTemplateEntity>().Do();
            }
        }
    }
}
