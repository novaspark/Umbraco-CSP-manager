using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.TagHelpers;

[HtmlTargetElement(Constants.TagHelper.ScriptTag, Attributes = CspHashAttributeName)]
public class CspScriptHashTagHelper : TagHelper
{
	private const string CspHashAttributeName = "csp-manager-add-hash";

	private readonly IScriptItemService _scriptItemService;
	private readonly ILogger<CspScriptHashTagHelper> _logger;

	public CspScriptHashTagHelper(IScriptItemService scriptItemService, ILogger<CspScriptHashTagHelper> logger)
	{
		_scriptItemService = scriptItemService;
		_logger = logger;
	}

	/// <summary>
	/// Specifies whether an <c>integrity</c> attribute (and the corresponding CSP <c>script-src</c> hash)
	/// should be added for this script, looked up from a <see cref="Models.ScriptItem"/> matching its <c>src</c>.
	/// </summary>
	[HtmlAttributeName(CspHashAttributeName)]
	public bool UseCspHash { get; set; }

	[HtmlAttributeNotBound, ViewContext]
	public ViewContext ViewContext { get; set; } = null!;

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		if (!UseCspHash)
		{
			return;
		}

		if (output.TagName != Constants.TagHelper.ScriptTag)
		{
			_logger.LogWarning("CSP Hash used on an invalid tag {Tag}", output.TagName);
			return;
		}

		var src = output.Attributes.FirstOrDefault(a => a.Name == "src")?.Value?.ToString();
		if (string.IsNullOrEmpty(src))
		{
			_logger.LogWarning("CSP Hash used on a script tag without a src attribute");
			return;
		}

		var hash = await _scriptItemService.GetHashAsync(src, ViewContext.HttpContext.RequestAborted);
		if (hash is null)
		{
			return;
		}

		output.Attributes.Add(new TagHelperAttribute("integrity", hash));

		if (!output.Attributes.ContainsName("crossorigin"))
		{
			output.Attributes.Add(new TagHelperAttribute("crossorigin", "anonymous"));
		}

		ViewContext.HttpContext.Items[Constants.TagHelper.CspManagerScriptHashSet] = true;
	}
}
