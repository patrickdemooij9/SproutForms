using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    /// <summary>
    /// Adds the table for a form's history. Existing forms start with an empty history.
    /// </summary>
    internal class AddFormAuditMigration : AsyncMigrationBase
    {
        public AddFormAuditMigration(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            if (!TableExists("SproutForms_FormAudit"))
            {
                Create.Table<FormAuditEntryEntity>().Do();
            }
            return Task.CompletedTask;
        }
    }
}
