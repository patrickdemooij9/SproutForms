using NPoco;
using SproutForms.Core.Models;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Models.Database
{
    [TableName("SproutForms_Forms")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class FormEntity
    {
        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = false)]
        public Guid Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("Alias")]
        public string Alias { get; set; }

        [Column("Source")]
        public int Source { get; set; }

        [Column("FolderId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? FolderId { get; set; }

        [Column("TrashedAt")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? TrashedAt { get; set; }

        [Column("TrashedBy")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? TrashedBy { get; set; }
    }
}
