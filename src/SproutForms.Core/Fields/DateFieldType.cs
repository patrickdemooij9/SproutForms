using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;

namespace SproutForms.Core.Fields
{
    public class DateFieldType : FormFieldBase<DateFieldConfig, DateTime>
    {
        public override Guid Id => Guid.Parse("105d693d-26fe-4935-b03f-adf7b1c74e6d");

        public override string Alias => "date";

        public override string DisplayName => "Date";

        public override DateFieldConfig DefaultConfiguration => new();

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
