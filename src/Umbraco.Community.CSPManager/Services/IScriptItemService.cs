using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Service for managing scripts whose Subresource Integrity hash is tracked by CSP Manager.
/// </summary>
public interface IScriptItemService
{
	/// <summary>
	/// Retrieves all script items, ordered by <see cref="ScriptItem.Src"/>.
	/// </summary>
	Task<IReadOnlyList<ScriptItem>> GetScriptItemsAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves a single script item by id, or <c>null</c> if it doesn't exist.
	/// </summary>
	Task<ScriptItem?> GetScriptItemAsync(Guid id, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves the distinct set of currently-known script hashes, used to populate the
	/// <c>script-src</c> directive. Backed by the runtime cache; invalidated on save/delete/resync.
	/// </summary>
	Task<IReadOnlyCollection<string>> GetCachedScriptHashesAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Looks up the current hash for a specific script <c>src</c>, or <c>null</c> if unknown.
	/// </summary>
	Task<string?> GetHashAsync(string src, CancellationToken cancellationToken);

	/// <summary>
	/// Creates or updates a script item. When <paramref name="item"/> is new (its <see cref="ScriptItem.Id"/>
	/// is <see cref="Guid.Empty"/>), an id is assigned and its hash is generated immediately.
	/// </summary>
	Task<ScriptItem> SaveScriptItemAsync(ScriptItem item, CancellationToken cancellationToken);

	/// <summary>
	/// Re-downloads <see cref="ScriptItem.Src"/> and regenerates its stored hash.
	/// </summary>
	Task<ScriptItem> RegenerateHashAsync(Guid id, CancellationToken cancellationToken);

	Task DeleteScriptItemAsync(Guid id, CancellationToken cancellationToken);

	/// <summary>
	/// Regenerates the hash for every script item flagged <see cref="ScriptItem.SynchroniseOnStartup"/>.
	/// A failure generating one item's hash is logged and does not stop the others.
	/// </summary>
	Task ResyncScriptItemsAsync(CancellationToken cancellationToken);
}
