namespace Umbraco.Community.CSPManager.Controllers;

using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Community.CSPManager;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

[PluginController(CspConstants.PluginAlias)]
public sealed class CSPManagerApiController : UmbracoAuthorizedJsonController
{
	private readonly ICspService _cspService;
	private readonly IScriptItemService _scriptItemService;

	public CSPManagerApiController(ICspService cspService, IScriptItemService scriptItemService)
	{
		_cspService = cspService;
		_scriptItemService = scriptItemService;
	}

	[HttpGet]
	public CspDefinition GetDefinition(bool isBackOffice = false) => _cspService.GetCspDefinition(isBackOffice);

	[HttpGet]
#pragma warning disable CA1822 // Mark members as static
	public ICollection<string> GetCspDirectiveOptions() => CspConstants.AllDirectives.ToArray();
#pragma warning restore CA1822 // Mark members as static

	[HttpPost]
	public async Task<CspDefinition> SaveDefinition(CspDefinition definition)
	{
		if (definition.Id == Guid.Empty)
		{
			throw new ArgumentOutOfRangeException(nameof(definition), "Definition Id is blank");
		}

		return await _cspService.SaveCspDefinitionAsync(definition);
	}

	[HttpGet]
	public async Task<IList<ScriptItem>> SearchScriptItems()
	{
		return await _scriptItemService.FindAllScriptItems();
	}


	[HttpGet]
	public async Task<IList<ScriptItem>> GetSavedScriptItems()
	{
		return await _scriptItemService.GetSavedScriptItems();
	}

	[HttpPost]
	public async Task<ScriptItem> AddScriptItem(ScriptItem scriptItem)
	{
		if (scriptItem.Id == Guid.Empty)
		{
			throw new ArgumentOutOfRangeException(nameof(scriptItem), "ScriptItem Id is blank");
		}

		return await _scriptItemService.Add(scriptItem);
	}

	[HttpPost]
	public async Task<ScriptItem> UpdateScriptItem(UpdateModel model)
	{
		return await _scriptItemService.Update(model.Id, model.Description, model.SynchroniseOnStartup);
	}


	[HttpPost]
	public async Task<ScriptItem> UpdateScriptItemHash(UpdateModel model)
	{
		return await _scriptItemService.UpdateHash(model.Id, model.Hash);
	}

	[HttpPost]
	public async Task DeleteScriptItem(UpdateModel model)
	{
		await _scriptItemService.Delete(model.Id);
	}
}
