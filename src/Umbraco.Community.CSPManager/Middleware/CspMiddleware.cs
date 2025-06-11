namespace Umbraco.Community.CSPManager.Middleware;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Extensions;

public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;
	private readonly IScriptItemService _scriptItemService;
	private readonly IEventAggregator _eventAggregator;

	public CspMiddleware(
		RequestDelegate next,
		IRuntimeState runtimeState,
		ICspService cspService,
		IScriptItemService scriptItemService,
		IEventAggregator eventAggregator)
	{
		_next = next;
		_runtimeState = runtimeState;
		_cspService = cspService;
		_scriptItemService = scriptItemService;
		_eventAggregator = eventAggregator;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (_runtimeState.Level != RuntimeLevel.Run)
		{
			await _next(context);
			return;
		}

		context.Response.OnStarting(async () =>
		{
			var definition = _cspService.GetCachedCspDefinition(context.Request.IsBackOfficeRequest());

			await _eventAggregator.PublishAsync(new CspWritingNotification(definition, context));

			if (definition is not { Enabled: true })
			{

				return;
			}

			var csp = ConstructCspDictionary(definition, context);
			var cspValue = string.Join(";", csp.Select(x => x.Key + " " + x.Value));

			if (!string.IsNullOrEmpty(cspValue))
			{
				context.Response.Headers.Append(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName, cspValue);
			}

			var scriptHashes = await GetScriptHashes(definition, context);
			if (scriptHashes!=null)
			{
				context.Response.Headers.Append(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName, $"{CspConstants.Directives.ScriptSource} {scriptHashes}");
			}
		});

		await _next(context);
	}

	private Dictionary<string, string> ConstructCspDictionary(CspDefinition definition, HttpContext httpContext)
	{
		string? scriptNonce = null;
		if (httpContext.GetItem<string>(CspConstants.CspManagerScriptNonceSet) == "set")
		{
			scriptNonce = _cspService.GetCspScriptNonce(httpContext);
		}

		string? styleNonce = null;
		if (httpContext.GetItem<string>(CspConstants.CspManagerStyleNonceSet) == "set")
		{
			styleNonce = _cspService.GetCspStyleNonce(httpContext);
		}

		var csp = definition.Sources
		.SelectMany(c => c.Directives.Select(d => new { Directive = d, c.Source }))
		.GroupBy(x => x.Directive)
		.ToDictionary(g => g.Key, g => string.Join(" ", g.Select(x => x.Source)));

		if (!string.IsNullOrWhiteSpace(definition.ReportingDirective) && !string.IsNullOrWhiteSpace(definition.ReportUri))
		{
			csp.TryAdd(definition.ReportingDirective, definition.ReportUri);
		}

		if (!string.IsNullOrWhiteSpace(scriptNonce) && csp.TryGetValue(CspConstants.Directives.ScriptSource, out var scriptSrc))
		{
			scriptSrc += $" 'nonce-{scriptNonce}'";
			csp[CspConstants.Directives.ScriptSource] = scriptSrc;
		}

		if (!string.IsNullOrWhiteSpace(styleNonce) && csp.TryGetValue(CspConstants.Directives.StyleSource, out var styleSrc))
		{
			styleSrc += $" 'nonce-{styleNonce}'";
			csp[CspConstants.Directives.StyleSource] = styleSrc;
		}

		return csp;
	}

	private async Task<string?> GetScriptHashes(CspDefinition definition, HttpContext httpContext)
	{
		// Script hashes - add as a separate directive for clarity
		if (httpContext.GetItem<string>(CspConstants.CspManagerScriptHashSet) == "set")
		{
			var hashes = await _scriptItemService.GetCachedScriptItemsDictionary();
			if (hashes != null)
			{
				return string.Join(' ', hashes.Select(x => $"'{x.Value}'"));
			}
		}
		return null;
	}
}
