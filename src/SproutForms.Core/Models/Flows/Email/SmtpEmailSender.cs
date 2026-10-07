using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace SproutForms.Core.Models.Flows.Email
{
    /// <summary>
    /// Sends the email workflow's emails with SMTP, for sites without Umbraco. Umbraco sites use Umbraco's own email settings instead.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IOptionsMonitor<SmtpEmailOptions> _options;
        private readonly IWebHostEnvironment _env;

        public SmtpEmailSender(IOptionsMonitor<SmtpEmailOptions> options, IWebHostEnvironment env)
        {
            _options = options;
            _env = env;
        }

        public async Task SendAsync(string from, string to, string subject, string body, CancellationToken ct)
        {
            var options = _options.CurrentValue;
            using var client = CreateClient(options);
            using var message = new MailMessage(from, to, subject, body) { IsBodyHtml = true };
            await client.SendMailAsync(message, ct);
        }

        private SmtpClient CreateClient(SmtpEmailOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.PickupDirectory))
            {
                var directory = Path.Combine(_env.ContentRootPath, options.PickupDirectory);
                Directory.CreateDirectory(directory);
                return new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                    PickupDirectoryLocation = directory
                };
            }

            if (string.IsNullOrWhiteSpace(options.Host))
            {
                throw new InvalidOperationException("SproutForms:Smtp:Host or SproutForms:Smtp:PickupDirectory must be configured to send emails.");
            }

            var client = new SmtpClient(options.Host, options.Port) { EnableSsl = options.EnableSsl };
            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                client.Credentials = new NetworkCredential(options.UserName, options.Password);
            }
            return client;
        }
    }
}
