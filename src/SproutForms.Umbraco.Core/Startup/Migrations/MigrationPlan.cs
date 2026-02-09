using Umbraco.Cms.Core.Packaging;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    public class MigrationPlan : PackageMigrationPlan
    {
        public MigrationPlan() : base("SproutForms", "SproutForms")
        {
        }

        protected override void DefinePlan()
        {
            To<FormsInitialMigration>("v1");
            To<FormsUserGroupMigration>("v2");
        }
    }
}
