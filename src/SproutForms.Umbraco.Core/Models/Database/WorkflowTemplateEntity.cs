using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Models.Database
{
    [TableName("SproutForms_WorkflowTemplates")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class WorkflowTemplateEntity
    {
        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = false)]
        public Guid Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("WorkflowTypeAlias")]
        public string WorkflowTypeAlias { get; set; } = string.Empty;

        [Column("ConfigurationJson")]
        public string ConfigurationJson { get; set; } = "{}";

        [Column("LockedFieldsJson")]
        public string LockedFieldsJson { get; set; } = "[]";

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
