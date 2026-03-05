using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.Email;
using IEmailSender = Umbraco.Cms.Core.Mail.IEmailSender;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class UmbracoEmailSender : SproutForms.Core.Models.Flows.Email.IEmailSender
    {
        private readonly IEmailSender _sender;
        private readonly GlobalSettings _globalSettings;

        public UmbracoEmailSender(IEmailSender sender, IOptionsMonitor<GlobalSettings> globalSettings)
        {
            _sender = sender;
            _globalSettings = globalSettings.CurrentValue;
        }

        public Task SendAsync(string from, string to, string subject, string body, CancellationToken ct)
        {
            if (!_globalSettings.IsSmtpServerConfigured && !_globalSettings.IsPickupDirectoryLocationConfigured)
            {
                throw new Exception("SMTP server or pickup directory location must be configured to send emails.");
            }

            return _sender.SendAsync(new EmailMessage(from, to, subject, body, true), "forms-mail");
        }
    }
}
