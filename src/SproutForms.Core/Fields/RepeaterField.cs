using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class RepeaterField : FormFieldBase<RepeaterFieldConfig, RepeaterItemValue[]>
    {
        public override string Alias => "repeater";

        public override RepeaterFieldConfig DefaultConfiguration => new()
        {
            Fields = new List<FormField>()
        };

        protected override ValidationResult Validate(RepeaterItemValue[] value, RepeaterFieldConfig configuration)
        {
            if (configuration.MinItems is not null && value.Length < configuration.MinItems)
                return ValidationResult.Fail(
                    $"At least {configuration.MinItems} items required");

            if (configuration.MaxItems is not null && value.Length > configuration.MaxItems)
                return ValidationResult.Fail(
                    $"At most {configuration.MaxItems} items allowed");

            return ValidationResult.Success();
        }
    }
}
