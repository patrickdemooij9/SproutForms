using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class CheckboxFieldType : FormFieldBase<CheckboxFieldConfig, bool>, IFormTypeRequiredHandler
    {
        public override string Alias => "checkbox";

        public override CheckboxFieldConfig DefaultConfiguration => new();

        public override bool RendersOwnLabel => true;

        public ValidationResult CheckForRequired(string value)
        {
            if (value != "true")
            {
                return ValidationResult.Fail("This field is required.");
            }
            return ValidationResult.Success();
        }

        protected override ValidationResult Validate(bool value, CheckboxFieldConfig configuration)
        {
            return ValidationResult.Success();
        }
    }
}
