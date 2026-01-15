using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class TextAreaFieldType : FormFieldBase<TextAreaConfig, string>
    {
        public override Guid Id => Guid.Parse("b5bc2a51-a54c-4efb-8d97-f7c72436b69b");

        public override string Alias => "textarea";

        public override string DisplayName => "Textarea";

        public override TextAreaConfig DefaultConfiguration => new();

        protected override ValidationResult Validate(string value, TextAreaConfig configuration)
        {
            if (configuration.MaxLength is not null && value.Length > configuration.MaxLength)
                return ValidationResult.Fail($"Maximum length is {configuration.MaxLength}");

            return ValidationResult.Success();
        }
    }
}
