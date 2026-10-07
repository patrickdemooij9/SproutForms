namespace SproutForms.Core.Models.Flows.Email
{
    /// <summary>
    /// Settings bound from "SproutForms:Smtp", for sites without Umbraco. Set either a host or a pickup directory.
    /// </summary>
    public class SmtpEmailOptions
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }

        /// <summary>
        /// Writes each email as a file to this folder instead of sending it, such as while developing. Relative to the content root.
        /// </summary>
        public string? PickupDirectory { get; set; }
    }
}
