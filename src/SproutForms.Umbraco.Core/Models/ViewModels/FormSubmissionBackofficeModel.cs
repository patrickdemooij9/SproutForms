using System;
using System.Collections.Generic;
using System.Text;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormSubmissionBackofficeModel
    {
        public Guid Id { get; set; }
        public FormSubmissionValueBackofficeModel[] Values { get; set; } = [];
        public List<WorkflowStageStatusModel> WorkflowStages { get; set; } = [];
    }
}
