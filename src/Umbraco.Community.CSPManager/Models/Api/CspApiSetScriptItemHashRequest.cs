using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// Request body for manually setting a script item's hash.
/// </summary>
public sealed class CspApiSetScriptItemHashRequest
{
	[Required]
	public string Hash { get; set; } = string.Empty;
}
