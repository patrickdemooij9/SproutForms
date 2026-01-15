using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormFieldBackofficeModel
    {
        public string Alias { get; set; }
        public string Label { get; set; }
        public Guid FieldTypeId { get; set; }
        public bool Required { get; set; }

        public Dictionary<string, string> Configuration { get; set; }
        public FieldConditions? Conditions { get; set; }

        public FormFieldBackofficeModel(FormField field)
        {
            Alias = field.Alias;
            Label = field.Label;
            FieldTypeId = field.FieldTypeId;
            Required = field.Required;
            Conditions = field.Conditions;

            Configuration = [];
        }

        public FormFieldBackofficeModel() // JSON constructor
        {
            
        }
    }
}
