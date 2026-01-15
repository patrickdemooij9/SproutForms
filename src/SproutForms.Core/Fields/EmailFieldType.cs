using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace SproutForms.Core.Fields
{
    public class EmailFieldType : FormFieldBase<EmailFieldConfig, string>
    {
        public override Guid Id => Guid.Parse("3e07b933-349c-408c-b337-2f983cd2938f");

        public override string Alias => "email";

        public override string DisplayName => "Email";

        public override EmailFieldConfig DefaultConfiguration => new();

        protected override ValidationResult Validate(string value, EmailFieldConfig configuration)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Success(); // required handled elsewhere

            try
            {
                _ = new MailAddress(value);
                return ValidationResult.Success();
            }
            catch
            {
                return ValidationResult.Fail("Invalid email address");
            }
        }
    }
}
