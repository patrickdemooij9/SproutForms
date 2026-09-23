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
        public override string Alias => "text";

        public override TextFieldConfig DefaultConfiguration => new TextFieldConfig();

        protected override IEnumerable<ValidationRule> GetValidationRulesCore(TextFieldConfig config)
        {
            if (config.MinLength.HasValue)
            {
                yield return new ValidationRule
                {
                    Type = "minLength",
                    Value = config.MinLength.Value,
                    Message = $"The field must be at least {config.MinLength.Value} characters long."
                };
            }

            if (config.MaxLength.HasValue)
            {
                yield return new ValidationRule
                {
                    Type = "maxLength",
                    Value = config.MaxLength.Value,
                    Message = $"The field must be no more than {config.MaxLength.Value} characters long."
                };
            }

            if (!string.IsNullOrWhiteSpace(config.Regex))
            {
                yield return new ValidationRule
                {
                    Type = "regex",
                    Value = config.Regex,
                    Message = "Invalid format"
                };
            }
        }

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
            if (!string.IsNullOrWhiteSpace(config.Regex) && !MatchesRegex(value, config.Regex))
            {
                return ValidationResult.Fail("Invalid format");
            }
            return ValidationResult.Success();
        }

        // The pattern comes from the form's configuration and the input from the visitor; a pattern that backtracks
        // badly would otherwise hold the request thread for as long as the input makes it take
        private static bool MatchesRegex(string value, string pattern)
        {
            try
            {
                return Regex.IsMatch(value, pattern, RegexOptions.None, RegexMatchTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(500);
    }
}
