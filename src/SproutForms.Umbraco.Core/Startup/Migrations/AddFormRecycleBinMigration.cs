using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    /// <summary>
    /// Adds the columns that put a form in the recycle bin.
    /// </summary>
    internal class AddFormRecycleBinMigration : AsyncMigrationBase
    {
        public AddFormRecycleBinMigration(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            // From the entity's attributes, so the column types fit each database provider
            if (!ColumnExists("SproutForms_Forms", "TrashedAt"))
            {
                AddColumn<FormEntity>("TrashedAt");
            }
            if (!ColumnExists("SproutForms_Forms", "TrashedBy"))
            {
                AddColumn<FormEntity>("TrashedBy");
            }
            return Task.CompletedTask;
        }
    }
}
