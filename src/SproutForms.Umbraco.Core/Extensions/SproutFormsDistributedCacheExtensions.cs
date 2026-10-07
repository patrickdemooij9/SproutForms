using SproutForms.Umbraco.Core.Caching;
using Umbraco.Cms.Core.Cache;

namespace SproutForms.Umbraco.Core.Extensions
{
    /// <summary>
    /// Clears the cached forms and form versions on every server, this one included
    /// </summary>
    public static class SproutFormsDistributedCacheExtensions
    {
        public static void RefreshSproutForms(this DistributedCache distributedCache)
            => distributedCache.RefreshAll(SproutFormsCacheRefresher.UniqueId);
    }
}
