using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace Prototype.Web.Cms;

public sealed class CmsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ContentSeeder>();
}
