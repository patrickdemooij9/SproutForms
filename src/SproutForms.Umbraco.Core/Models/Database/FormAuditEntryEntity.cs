using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Models.Database
{
    [TableName("SproutForms_FormAudit")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class FormAuditEntryEntity
    {
        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = false)]
        public Guid Id { get; set; }

        [Column("FormId")]
        [ForeignKey(typeof(FormEntity), Column = "Id")]
        [Index(IndexTypes.NonClustered, Name = "IX_SproutForms_FormAudit_FormId")]
        public Guid FormId { get; set; }

        [Column("Action")]
        public int Action { get; set; }

        [Column("UserKey")]
        public string UserKey { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        // No foreign key: the entry stays readable when its version is ever cleaned up
        [Column("VersionId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? VersionId { get; set; }

        [Column("Comment")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string? Comment { get; set; }
    }
}
