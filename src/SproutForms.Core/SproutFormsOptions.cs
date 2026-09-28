namespace SproutForms.Core
{
    /// <summary>
    /// Settings bound from the "SproutForms" configuration section.
    /// </summary>
    public class SproutFormsOptions
    {
        /// <summary>
        /// Stores the visitor's IP address with each submission. Off by default: an IP address is personal data under the GDPR.
        /// Behind a proxy or load balancer, configure the forwarded headers middleware, or the proxy's address is stored instead.
        /// </summary>
        public bool StoreIpAddress { get; set; }

        /// <summary>
        /// The theme forms render with when the page doesn't choose one: a folder under ~/Views/Forms/Themes/. Empty uses the default views.
        /// </summary>
        public string? DefaultTheme { get; set; }

        public RecycleBinOptions RecycleBin { get; set; } = new();
    }

    public class RecycleBinOptions
    {
        /// <summary>
        /// Days an item stays in the recycle bin before it is deleted for good. 0 keeps it until someone deletes it.
        /// Submissions hold personal data, so they shouldn't stay in the bin forever.
        /// </summary>
        public int RetentionDays { get; set; } = 30;
    }
}
