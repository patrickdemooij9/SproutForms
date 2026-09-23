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
    }
}
