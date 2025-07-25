﻿namespace Umbraco.Community.CSPManager.TagHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Services;

[HtmlTargetElement(ScriptTag, Attributes = "csp-manager-*")]
public class CspScriptHashTagHelper : TagHelper
{
	private const string ScriptTag = "script";
	private const string CspHashAttributeName = "csp-manager-add-hash";
	private const string CspAddVersionAttributeName = "csp-manager-add-version-qs";

	private readonly IScriptItemService _scriptItemService;
	private readonly ILogger<CspScriptHashTagHelper> _logger;

	public CspScriptHashTagHelper(IScriptItemService scriptItemService, ILogger<CspScriptHashTagHelper> logger)
	{
		_scriptItemService = scriptItemService;
		_logger = logger;
	}

	[HtmlAttributeName(CspHashAttributeName)]
	public bool UseCspHash { get; set; }

	[HtmlAttributeName(CspAddVersionAttributeName)]
	public bool AddVersionQueryString { get; set; }

	[HtmlAttributeNotBound, ViewContext]
	public ViewContext ViewContext { get; set; } = null!;

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		if (!UseCspHash && !AddVersionQueryString)
		{
			return; 
		}

		if (output.TagName == ScriptTag)
		{
			var src = output.Attributes.FirstOrDefault(a => a.Name == "src")?.Value?.ToString();
			if (string.IsNullOrEmpty(src))
			{
				_logger.LogWarning("CSP Hash used on a script tag without a src attribute");
				return;
			}

			if (UseCspHash) 
			{
				var hash = await _scriptItemService.GetHash(src);
				if (hash != null)
				{
					output.Attributes.Add(new TagHelperAttribute("integrity", hash));
					var co = output.Attributes.FirstOrDefault(a => a.Name.Equals("crossorigin", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
					if (co == null)
					{
						//TODO: Should we check for non-local script files?
						output.Attributes.Add(new TagHelperAttribute("crossorigin", "anonymous"));
					}

					var httpContext = ViewContext.HttpContext;
					if (string.IsNullOrEmpty(httpContext.GetItem<string>(CspConstants.CspManagerScriptHashSet)))
					{
						httpContext.SetItem(CspConstants.CspManagerScriptHashSet, "set");
					}
				}
			}			

			if (AddVersionQueryString)
			{
				var av = Assembly.GetExecutingAssembly().GetName()?.Version?.ToString();
				output.Attributes.RemoveAll("src");
				output.Attributes.Add(new TagHelperAttribute("src", src + (src.Contains('?') ? "&" : "?") + "v=" + av));
			}
		}
		else
		{
			_logger.LogWarning("CSP Hash used on an invalid tag {Tag}", output.TagName);
			return;
		}		
	}
}
