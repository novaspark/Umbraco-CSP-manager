namespace Umbraco.Community.CSPManager.Notifications.Handlers;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;
using Umbraco.Community.CSPManager.Services;

internal sealed class CspApplicationStartedNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
	private readonly IAppPolicyCache _runtimeCache;
	private readonly IScriptItemService _scriptItemService;
	private readonly DistributedCache _distributedCache;
	public CspApplicationStartedNotificationHandler(
		IScriptItemService scriptItemService,
		AppCaches appCaches,
		DistributedCache distributedCache
	)
	{
		_scriptItemService = scriptItemService;
		_runtimeCache = appCaches.RuntimeCache;
		_distributedCache = distributedCache;
	}

	public void Handle(UmbracoApplicationStartedNotification notification)
	{
		// Regenerate hashes for all script items that require it
		_scriptItemService.ReSyncScriptItems().GetAwaiter().GetResult();
	}
}
