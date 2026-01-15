using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class RadioFieldType : FormFieldBase<RadioFieldConfig, string>
    {
        public override Guid Id => Guid.Parse("e2ed7feb-8d39-4e5e-a47a-4456f278c8c6");

        public override string Alias => "radio";

        public override string DisplayName => "Radio field";

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
