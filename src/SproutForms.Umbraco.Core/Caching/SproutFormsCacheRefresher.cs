using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;

namespace SproutForms.Umbraco.Core.Caching
{
    /// <summary>
    /// Clears the cached forms and form versions on every server when one of them changes, or a workflow template they copy values
    /// from. Without it, a load balanced site serves the old form from its other servers until their cache expires.
    /// </summary>
    public sealed class SproutFormsCacheRefresher : CacheRefresherBase<SproutFormsCacheRefresherNotification>
    {
        public static readonly Guid UniqueId = new("6f3b2c1e-8a4d-4f57-9a1b-3c5d7e9f0a21");

        // Every key the repositories cache under starts with it
        public const string CacheKeyPrefix = "sproutForms_";

        public SproutFormsCacheRefresher(AppCaches appCaches, IEventAggregator eventAggregator, ICacheRefresherNotificationFactory factory)
            : base(appCaches, eventAggregator, factory)
        {
        }

        public override Guid RefresherUniqueId => UniqueId;

        public override string Name => "SproutForms cache refresher";

        // Forms change rarely, so clearing all of them is simpler than tracking which cached form and version lists one change touches
        public override void RefreshAll()
        {
            AppCaches.RuntimeCache.ClearByKey(CacheKeyPrefix);
            base.RefreshAll();
        }

        public override void Refresh(int id) => RefreshAll();

        public override void Refresh(Guid id) => RefreshAll();

        public override void Remove(int id) => RefreshAll();
    }
}
