using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    /// <summary>
    /// Adds the columns that put a submission in its form's recycle bin.
    /// </summary>
    internal class AddSubmissionRecycleBinMigration : AsyncMigrationBase
    {
        public AddSubmissionRecycleBinMigration(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            // From the entity's attributes, so the column types fit each database provider
            if (!ColumnExists("SproutForms_FormSubmissions", "TrashedAt"))
            {
                AddColumn<FormSubmissionEntity>("TrashedAt");
            }
            if (!ColumnExists("SproutForms_FormSubmissions", "TrashedBy"))
            {
                AddColumn<FormSubmissionEntity>("TrashedBy");
            }
            return Task.CompletedTask;
        }
    }
}
