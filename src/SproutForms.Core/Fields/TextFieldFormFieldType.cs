using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SproutForms.Core.Fields
{
    public class TextFieldFormFieldType : FormFieldBase<TextFieldConfig, string>
    {
        public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-4789-9012-3456789abcde");

        public override string Alias => "text";

        public override string DisplayName => "Text Field";

        public override TextFieldConfig DefaultConfiguration => new TextFieldConfig();

        protected override ValidationResult Validate(string value, TextFieldConfig config)
        {
            if (config.MinLength.HasValue && value.Length < config.MinLength.Value)
            {
                return ValidationResult.Fail($"The field must be at least {config.MinLength.Value} characters long.");
            }
            if (config.MaxLength.HasValue && value.Length > config.MaxLength.Value)
            {
                return ValidationResult.Fail($"The field must be no more than {config.MaxLength.Value} characters long.");
            }
            if (!string.IsNullOrWhiteSpace(config.Regex) && !Regex.IsMatch(value, config.Regex))
            {
                return ValidationResult.Fail("Invalid format");
            }
            return ValidationResult.Success();
        }
    }
}
