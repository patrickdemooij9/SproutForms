using Microsoft.AspNetCore.Http;

namespace SproutForms.Core.Helpers
{
    public static class SameHostUrl
    {
        /// <summary>
        /// Returns the URL when it is an absolute http(s) URL on the host of the given request, otherwise null.
        /// </summary>
        public static string? GetOrNull(string? url, HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return null;

            return string.Equals(uri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase)
                ? url
                : null;
        }
    }
}
