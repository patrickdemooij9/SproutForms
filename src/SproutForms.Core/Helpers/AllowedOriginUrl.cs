namespace SproutForms.Core.Helpers
{
    public static class AllowedOriginUrl
    {
        /// <summary>
        /// Returns the URL when it is an absolute http(s) URL on one of the given origins, such as https://www.example.com, otherwise null.
        /// </summary>
        public static string? GetOrNull(string? url, IEnumerable<string> allowedOrigins)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return null;

            var origin = uri.GetLeftPart(UriPartial.Authority);
            return allowedOrigins.Any(it => string.Equals(it.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase))
                ? url
                : null;
        }
    }
}
