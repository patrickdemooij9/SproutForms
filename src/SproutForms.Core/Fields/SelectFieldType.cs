using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class SelectFieldType : FormFieldBase<SelectFieldConfig, string>
    {
        public override Guid Id => Guid.Parse("b566d340-dd8a-4074-b4a6-038b2631de69");

        public override string Alias => "select";

        public override string DisplayName => "Select";

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
