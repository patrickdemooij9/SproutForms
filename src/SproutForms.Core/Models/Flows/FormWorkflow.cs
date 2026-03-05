using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models.Flows
{
    public class FormWorkflow
    {
        public required string Alias { get; set; }

        public required string WorkflowTypeAlias { get; set; }
        public required object Configuration { get; set; }

        public int Order { get; set; }
        
        public Guid? TemplateId { get; set; }
    }
}
