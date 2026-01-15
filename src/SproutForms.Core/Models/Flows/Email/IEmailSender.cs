namespace SproutForms.Core.Models.Flows.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string from, string to, string subject, string body, CancellationToken ct);
    }
}
