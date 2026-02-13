using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    internal class AddFoldersMigration : MigrationBase
    {
        public AddFoldersMigration(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            if (!TableExists("SproutForms_Folders"))
            {
                Create.Table<FolderEntity>().Do();
            }

            if (!ColumnExists("SproutForms_Forms", "FolderId"))
            {
                Alter.Table("SproutForms_Forms")
                    .AddColumn("FolderId")
                    .AsGuid()
                    .Nullable()
                    .Do();
            }
        }
    }
}
