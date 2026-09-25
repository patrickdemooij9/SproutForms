using SproutForms.Umbraco.Core.Models.Database;
using System.Text.Json.Nodes;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Startup.Migrations
{
    /// <summary>
    /// Moves the rows of every stored definition into a single page, the layout used since forms can have multiple pages.
    /// </summary>
    internal class WrapRowsInPagesMigration : MigrationBase
    {
        public WrapRowsInPagesMigration(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            // Every version, not just the published one: older submissions still load the version they were made with
            var versions = Database.Fetch<FormVersionEntity>(Sql().SelectAll().From<FormVersionEntity>());
            foreach (var version in versions)
            {
                var migrated = WrapRowsInPage(version.DefinitionJson);
                if (migrated == null) continue;

                Database.Execute("UPDATE SproutForms_FormVersions SET DefinitionJson = @0 WHERE Id = @1", migrated, version.Id);
            }
        }

        // Edits the JSON directly, so the migration doesn't depend on the model or on the registered field, workflow and outcome types
        private static string? WrapRowsInPage(string definitionJson)
        {
            if (JsonNode.Parse(definitionJson) is not JsonObject definition) return null;
            if (definition.ContainsKey("Pages") || !definition.ContainsKey("Rows")) return null;

            // Rebuilt property by property, so Pages takes the place of Rows
            var migrated = new JsonObject();
            foreach (var (name, value) in definition)
            {
                if (name != "Rows")
                {
                    migrated[name] = value?.DeepClone();
                    continue;
                }

                migrated["Pages"] = new JsonArray(new JsonObject
                {
                    ["Title"] = null,
                    ["Rows"] = value?.DeepClone() ?? new JsonArray(),
                    ["NextLabel"] = null,
                    ["PreviousLabel"] = null,
                    ["Visibility"] = null
                });
            }
            return migrated.ToJsonString();
        }
    }
}
