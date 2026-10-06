using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    /// <summary>
    /// Adds the column for the variables a form's calculations work out.
    /// </summary>
    internal class AddSubmissionVariablesMigration : AsyncMigrationBase
    {
        public AddSubmissionVariablesMigration(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            // From the entity's attributes, so the column type fits each database provider
            if (!ColumnExists("SproutForms_FormSubmissions", "VariablesJson"))
            {
                AddColumn<FormSubmissionEntity>("VariablesJson");
            }
            return Task.CompletedTask;
        }
    }
}
