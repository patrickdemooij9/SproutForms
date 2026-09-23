using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    internal class AddSubmissionResultsMigration : MigrationBase
    {
        public AddSubmissionResultsMigration(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            if (!ColumnExists("SproutForms_FormSubmissions", "ResultsJson"))
            {
                // From the entity's attributes, so the column type fits each database provider
                AddColumn<FormSubmissionEntity>("ResultsJson");
            }
        }
    }
}
