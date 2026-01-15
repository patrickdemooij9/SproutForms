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
    }
}
