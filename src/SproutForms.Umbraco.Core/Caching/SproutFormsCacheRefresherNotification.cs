using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace SproutForms.Umbraco.Core.Caching
{
    public sealed class SproutFormsCacheRefresherNotification : CacheRefresherNotification
    {
        public SproutFormsCacheRefresherNotification(object messageObject, MessageType messageType)
            : base(messageObject, messageType)
        {
        }
    }
}
