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
        public string FieldTypeAlias { get; set; }
        public bool Required { get; set; }

        public Dictionary<string, object?> Configuration { get; set; }
        public FieldConditions? Conditions { get; set; }

        // The settings the form's type adds to this field's type, when it extends it
        public Dictionary<string, object?>? Extension { get; set; }

        public FormFieldBackofficeModel(FormField field)
        {
            Alias = field.Alias;
            Label = field.Label;
            FieldTypeAlias = field.FieldTypeAlias;
            Required = field.Required;
            Conditions = field.Conditions;

            Configuration = [];
        }

        public FormFieldBackofficeModel() // JSON constructor
        {
            
        }
    }
}
