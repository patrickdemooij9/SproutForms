using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class SelectFieldType : FormFieldBase<SelectFieldConfig, string>
    {
        public override string Alias => "select";

        public override SelectFieldConfig DefaultConfiguration => new()
        {
            Options = []
        };

        protected override ValidationResult Validate(string value, SelectFieldConfig configuration)
        {
            if (!configuration.Options.Any(o => o.Value == value))
                return ValidationResult.Fail("Invalid selection");

            return ValidationResult.Success();
        }
    }
}
