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
        public override string Alias => "email";

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
