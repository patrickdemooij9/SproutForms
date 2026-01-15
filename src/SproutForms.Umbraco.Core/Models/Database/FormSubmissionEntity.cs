using NPoco;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Models.Database
{
    [TableName("SproutForms_FormSubmissions")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class FormSubmissionEntity
    {
        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = false)]
        public Guid Id { get; set; }

        [Column("FormVersionId")]
        [ForeignKey(typeof(FormVersionEntity), Column = "Id")]
        public Guid FormVersionId { get; set; }

        [Column("SubmittedAt")]
        public DateTime SubmittedAt { get; set; }

        [Column("IpAddress")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? IpAddress { get; set; }

        [Column("ValuesJson")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string ValuesJson { get; set; }
    }
}
