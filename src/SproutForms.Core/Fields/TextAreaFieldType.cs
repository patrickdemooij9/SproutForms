using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class TextAreaFieldType : FormFieldBase<TextAreaConfig, string>
    {
        public override string Alias => "textarea";

        public override TextAreaConfig DefaultConfiguration => new();

        protected override ValidationResult Validate(string value, TextAreaConfig configuration)
        {
            if (configuration.MaxLength is not null && value.Length > configuration.MaxLength)
                return ValidationResult.Fail($"Maximum length is {configuration.MaxLength}");

            return ValidationResult.Success();
        }
    }
}
