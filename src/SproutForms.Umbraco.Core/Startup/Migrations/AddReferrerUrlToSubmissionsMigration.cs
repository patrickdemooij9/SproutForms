using Umbraco.Cms.Infrastructure.Migrations;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    internal class AddReferrerUrlToSubmissionsMigration : MigrationBase
    {
        public AddReferrerUrlToSubmissionsMigration(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            if (!ColumnExists("SproutForms_FormSubmissions", "PageUrl"))
            {
                Alter.Table("SproutForms_FormSubmissions")
                    .AddColumn("PageUrl")
                    .AsString()
                    .Nullable()
                    .Do();
            }
        }
    }
}
