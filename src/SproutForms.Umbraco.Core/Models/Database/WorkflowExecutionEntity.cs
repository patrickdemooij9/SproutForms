using NPoco;
using SproutForms.Core.Models.Flows;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SproutForms.Umbraco.Core.Models.Database
{
    [TableName("SproutForms_WorkflowExecutions")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class WorkflowExecutionEntity
    {
        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column("Id")]
        public Guid Id { get; init; }

        [Column("SubmissionId")]
        [ForeignKey(typeof(FormSubmissionEntity), Column = "Id")]
        public Guid SubmissionId { get; init; }

        [Column("WorkflowAlias")]
        public string WorkflowAlias { get; init; } = default!;

        [Column("WorkflowTypeAlias")]
        public string WorkflowTypeAlias { get; init; } = default!;

        [Column("ConfigurationJson")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string ConfigurationJson { get; init; } = default!;

        [Column("Order")]
        public int Order { get; init; }

        [Column("Status")]
        public int Status { get; set; }

        [Column("AttemptCount")]
        public int AttemptCount { get; set; }

        [Column("LastError")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? LastError { get; set; }

        [Column("CreatedUtc")]
        public DateTime CreatedUtc { get; init; }

        [Column("NextAttemptUtc")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? NextAttemptUtc { get; init; }

        [Column("StartedUtc")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? StartedUtc { get; set; }

        [Column("CompletedUtc")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? CompletedUtc { get; set; }
    }
}
