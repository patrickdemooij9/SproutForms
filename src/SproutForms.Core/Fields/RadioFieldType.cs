using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class RadioFieldType : FormFieldBase<RadioFieldConfig, string>
    {
        public override string Alias => "radio";

        public override RadioFieldConfig DefaultConfiguration => new()
        {
            Options = []
        };

        protected override ValidationResult Validate(string value, RadioFieldConfig configuration)
        {
            if (!configuration.Options.Any(o => o.Value == value))
                return ValidationResult.Fail("Invalid selection");

            return ValidationResult.Success();
        }
    }
}
