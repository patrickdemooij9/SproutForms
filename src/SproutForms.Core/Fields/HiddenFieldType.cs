using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class HiddenFieldType : FormFieldBase<HiddenFieldConfig, string>
    {
        public override string Alias => "hidden";

        public override HiddenFieldConfig DefaultConfiguration => new();

        public override bool RendersOwnLabel => true;

        protected override ValidationResult Validate(string value, HiddenFieldConfig configuration)
        {
            if (!configuration.AllowOverrideFromClient && value != configuration.DefaultValue)
            {
                return ValidationResult.Fail("Not allowed to override hidden field value from client.");
            }

            return ValidationResult.Success();
        }
    }
}
