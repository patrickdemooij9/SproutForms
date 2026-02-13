using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Models.Database
{
    [TableName("SproutForms_Folders")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class FolderEntity
    {
        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = false)]
        public Guid Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("ParentId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? ParentId { get; set; }

        [Column("SortOrder")]
        public int SortOrder { get; set; }
    }
}
