using System.ComponentModel.DataAnnotations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// API data transfer object representing a script tracked by CSP Manager for Subresource Integrity.
/// </summary>
public sealed class CspApiScriptItem : IValidatableObject
{
	public Guid Id { get; set; }

	/// <summary>
	/// The script's <c>src</c> attribute value: an absolute URL, a protocol-relative URL, or a
	/// path relative to the site's web root.
	/// </summary>
	[Required]
	public string Src { get; set; } = string.Empty;

	public string? Description { get; set; }

	/// <summary>
	/// The last-generated Subresource Integrity hash. Read-only: set by the server when the item
	/// is saved or its hash is regenerated.
	/// </summary>
	public string? Hash { get; set; }

	public bool SynchroniseOnStartup { get; set; }

	public DateTime? LastUpdated { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (string.IsNullOrWhiteSpace(Src))
		{
			yield return new ValidationResult("Src is required", [nameof(Src)]);
		}
	}

	internal static CspApiScriptItem FromScriptItem(ScriptItem item) => new()
	{
		Id = item.Id,
		Src = item.Src,
		Description = item.Description,
		Hash = item.Hash,
		SynchroniseOnStartup = item.SynchroniseOnStartup,
		LastUpdated = item.LastUpdated,
	};

	internal ScriptItem ToScriptItem() => new()
	{
		Id = Id,
		Src = Src,
		Description = Description,
		SynchroniseOnStartup = SynchroniseOnStartup,
	};
}
