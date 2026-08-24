using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Community.CSPManager.Models;

/// <summary>
/// Represents a script whose Subresource Integrity hash is managed by CSP Manager.
/// </summary>
/// <remarks>
/// Scripts registered here have their <c>integrity</c>/<c>crossorigin</c> attributes stamped
/// by <see cref="TagHelpers.CspScriptHashTagHelper"/>, and their hash folded into the
/// <c>script-src</c> directive by <see cref="Middleware.CspMiddleware"/>.
/// </remarks>
[TableName(nameof(ScriptItem))]
[PrimaryKey(nameof(Id), AutoIncrement = false)]
public class ScriptItem
{
	[PrimaryKeyColumn(AutoIncrement = false)]
	public Guid Id { get; set; }

	/// <summary>
	/// The script's <c>src</c> attribute value: an absolute URL, a protocol-relative URL, or a
	/// path relative to the site's web root.
	/// </summary>
	[Length(2000)]
	public string Src { get; set; } = string.Empty;

	[Length(500)]
	[NullSetting(NullSetting = NullSettings.Null)]
	public string? Description { get; set; }

	/// <summary>
	/// The Subresource Integrity hash (e.g. <c>sha384-...</c>), or <c>null</c> if it has not
	/// been generated yet or the last generation attempt failed.
	/// </summary>
	[Length(200)]
	[NullSetting(NullSetting = NullSettings.Null)]
	public string? Hash { get; set; }

	/// <summary>
	/// When <c>true</c>, the hash is regenerated automatically on Umbraco application startup.
	/// </summary>
	public bool SynchroniseOnStartup { get; set; }

	[NullSetting(NullSetting = NullSettings.Null)]
	public DateTime? LastUpdated { get; set; }
}
