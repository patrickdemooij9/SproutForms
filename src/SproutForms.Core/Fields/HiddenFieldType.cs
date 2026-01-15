using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class HiddenFieldType : FormFieldBase<HiddenFieldConfig, string>
    {
        public override Guid Id => Guid.Parse("8951c929-07a2-4dcc-9a1b-dc41f9921a1c");

        public override string Alias => "hidden";

        public override string DisplayName => "Hidden";

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
