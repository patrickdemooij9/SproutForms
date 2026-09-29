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

        public HeadlessOptions Headless { get; set; } = new();
    }

    public class HeadlessOptions
    {
        /// <summary>
        /// Turns on the headless API under /umbraco/sproutforms/delivery/api/v1. Off by default: it lets anyone read a published form's
        /// structure and submit it without an antiforgery token, which a site that only renders forms with Razor doesn't need.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The origins (such as https://www.example.com) of the front-ends that call the headless API from a browser. They are allowed
        /// by CORS, and a submission's page URL is only stored when it is on this site or one of these origins.
        /// </summary>
        public string[] AllowedOrigins { get; set; } = [];

        /// <summary>
        /// When set, every headless request must send it in the Api-Key header. Only for a front-end that calls the API from its server;
        /// a key in browser code is public.
        /// </summary>
        public string? ApiKey { get; set; }
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
