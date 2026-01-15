using NPoco;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Models.Database
{
    [TableName("SproutForms_FormVersions")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class FormVersionEntity
    {
        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = false)]
        public Guid Id { get; set; }

        [Column("FormId")]
        [ForeignKey(typeof(FormEntity), Column = "Id")]
        public Guid FormId { get; set; }

        [Column("Version")]
        public int Version { get; set; }

        [Column("DefinitionJson")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string DefinitionJson { get; set; }

        [Column("DefinitionHash")]
        public string DefinitionHash { get; set; }

        [Column("Status")]
        public int Status { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("CreatedBy")]
        public string CreatedBy { get; set; }
    }
}
