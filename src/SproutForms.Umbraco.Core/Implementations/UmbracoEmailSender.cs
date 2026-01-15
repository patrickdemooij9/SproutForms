using SproutForms.Core.Models.Flows.Email;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using IEmailSender = Umbraco.Cms.Core.Mail.IEmailSender;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class UmbracoEmailSender : SproutForms.Core.Models.Flows.Email.IEmailSender
    {
        private readonly IEmailSender _sender;

        public UmbracoEmailSender(IEmailSender sender)
        {
            _sender = sender;
        }

        public Task SendAsync(string from, string to, string subject, string body, CancellationToken ct)
        {
            return _sender.SendAsync(new EmailMessage(from, to, subject, body, true), "forms-mail");
        }
    }
}
