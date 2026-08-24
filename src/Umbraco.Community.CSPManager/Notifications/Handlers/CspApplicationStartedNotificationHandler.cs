using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Notifications.Handlers;

internal sealed class CspApplicationStartedNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
	private readonly IScriptItemService _scriptItemService;

	public CspApplicationStartedNotificationHandler(IScriptItemService scriptItemService)
	{
		_scriptItemService = scriptItemService;
	}

	public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
		=> _scriptItemService.ResyncScriptItemsAsync(cancellationToken);
}
