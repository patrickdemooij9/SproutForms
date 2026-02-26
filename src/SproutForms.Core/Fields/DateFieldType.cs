using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;

namespace SproutForms.Core.Fields
{
    public class DateFieldType : FormFieldBase<DateFieldConfig, DateTime>
    {
        public override string Alias => "date";

        public override DateFieldConfig DefaultConfiguration => new();

        protected override IEnumerable<ValidationRule> GetValidationRulesCore(DateFieldConfig config)
        {
            if (config.Min.HasValue)
            {
                yield return new ValidationRule
                {
                    Type = "minDate",
                    Value = config.Min.Value.ToString("yyyy-MM-dd"),
                    Message = $"Date must be after {config.Min.Value:yyyy-MM-dd}"
                };
            }

            if (config.Max.HasValue)
            {
                yield return new ValidationRule
                {
                    Type = "maxDate",
                    Value = config.Max.Value.ToString("yyyy-MM-dd"),
                    Message = $"Date must be before {config.Max.Value:yyyy-MM-dd}"
                };
            }
        }

        protected override ValidationResult Validate(DateTime value, DateFieldConfig configuration)
        {
            if (configuration.Min is not null && value < configuration.Min)
                return ValidationResult.Fail($"Date must be after {configuration.Min:yyyy-MM-dd}");

            if (configuration.Max is not null && value > configuration.Max)
                return ValidationResult.Fail($"Date must be before {configuration.Max:yyyy-MM-dd}");

            return ValidationResult.Success();
        }
    }
}
