﻿namespace Umbraco.Community.CSPManager.Composing;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Community.CSPManager.Backoffice;
using Umbraco.Community.CSPManager.Middleware;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;
using Umbraco.Community.CSPManager.Services;

public sealed class CspManagerComposer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.ManifestFilters().Append<PackageManifestFilter>();
		builder.AddSection<CspManagementSection>();
		builder.Services.AddTransient<ICspService, CspService>();
		builder.Services.AddTransient<IScriptItemService, ScriptItemService>();
		builder.Services.Configure<UmbracoPipelineOptions>(options =>
		{
#if NET8_0_OR_GREATER
			options.AddFilter(new UmbracoPipelineFilter(
				CspConstants.PackageAlias,
				postPipeline: applicationBuilder =>
				{
					applicationBuilder.UseMiddleware<CspMiddleware>();
				}));
		});
#else
			options.AddFilter(new UmbracoPipelineFilter(
				CspConstants.PackageAlias,
				_ => { },
				applicationBuilder =>
				{
					applicationBuilder.UseMiddleware<CspMiddleware>();
				},
				_ => { }));
		});
#endif
		builder.Services.Configure<CspManagerSettings>(builder.Config.GetRequiredSection("CspManager"));
		builder.AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesHandler>();
		builder.AddNotificationHandler<CspSavedNotification, CspSavedNotificationHandler>();
		builder.AddNotificationHandler<UmbracoApplicationStartedNotification, CspApplicationStartedNotificationHandler>();
	}
}
