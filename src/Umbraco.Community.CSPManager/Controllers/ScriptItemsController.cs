using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.CSPManager.Models.Api;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;

/// <summary>
/// API controller for managing scripts tracked by CSP Manager for Subresource Integrity.
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "ScriptItems")]
public class ScriptItemsController : CspManagerControllerBase
{
	private readonly IScriptItemService _scriptItemService;

	public ScriptItemsController(IScriptItemService scriptItemService)
	{
		_scriptItemService = scriptItemService;
	}

	[HttpGet("ScriptItems")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(IEnumerable<CspApiScriptItem>), 200)]
	public async Task<ActionResult<IEnumerable<CspApiScriptItem>>> GetScriptItems(CancellationToken cancellationToken)
	{
		var items = await _scriptItemService.GetScriptItemsAsync(cancellationToken);
		return Ok(items.Select(CspApiScriptItem.FromScriptItem));
	}

	[HttpGet("ScriptItems/{id:guid}")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiScriptItem), 200)]
	[ProducesResponseType(404)]
	public async Task<ActionResult<CspApiScriptItem>> GetScriptItem(Guid id, CancellationToken cancellationToken)
	{
		var item = await _scriptItemService.GetScriptItemAsync(id, cancellationToken);
		return item is null ? NotFound() : Ok(CspApiScriptItem.FromScriptItem(item));
	}

	[HttpPost("ScriptItems/save")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiScriptItem), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> SaveScriptItem([FromBody] CspApiScriptItem scriptItem, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(new ValidationProblemDetails(ModelState));
		}

		var saved = await _scriptItemService.SaveScriptItemAsync(scriptItem.ToScriptItem(), cancellationToken);
		return Ok(CspApiScriptItem.FromScriptItem(saved));
	}

	[HttpPost("ScriptItems/{id:guid}/regenerate-hash")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiScriptItem), 200)]
	[ProducesResponseType(404)]
	public async Task<ActionResult<CspApiScriptItem>> RegenerateHash(Guid id, CancellationToken cancellationToken)
	{
		try
		{
			var item = await _scriptItemService.RegenerateHashAsync(id, cancellationToken);
			return Ok(CspApiScriptItem.FromScriptItem(item));
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	/// <summary>
	/// Sets a script item's hash directly, with no download or recomputation - for pinning to a
	/// value obtained elsewhere (e.g. a vendor's own published SRI hash) rather than trusting a
	/// local re-fetch.
	/// </summary>
	[HttpPost("ScriptItems/{id:guid}/hash")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiScriptItem), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> SetHash(Guid id, [FromBody] CspApiSetScriptItemHashRequest request, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(new ValidationProblemDetails(ModelState));
		}

		try
		{
			var item = await _scriptItemService.SetHashAsync(id, request.Hash, cancellationToken);
			return Ok(CspApiScriptItem.FromScriptItem(item));
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	[HttpDelete("ScriptItems/{id:guid}")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(200)]
	public async Task<IActionResult> DeleteScriptItem(Guid id, CancellationToken cancellationToken)
	{
		await _scriptItemService.DeleteScriptItemAsync(id, cancellationToken);
		return Ok();
	}
}
