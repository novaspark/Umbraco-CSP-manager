using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPoco.Expressions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Extensions;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Implementation of <see cref="IScriptItemService"/> that manages script items using
/// Umbraco's scoping and caching infrastructure, following the same conventions as <see cref="CspService"/>.
/// </summary>
internal sealed class ScriptItemService : IScriptItemService
{
	private const string CacheKey = "CspManagerScriptHashes";

	private readonly IScopeProvider _scopeProvider;
	private readonly IAppPolicyCache _runtimeCache;
	private readonly IWebHostEnvironment _webHostEnvironment;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<ScriptItemService> _logger;
	private CspManagerOptions _options;

	public ScriptItemService(
		IScopeProvider scopeProvider,
		AppCaches appCaches,
		IWebHostEnvironment webHostEnvironment,
		IHttpClientFactory httpClientFactory,
		IOptionsMonitor<CspManagerOptions> options,
		ILogger<ScriptItemService> logger)
	{
		_scopeProvider = scopeProvider;
		_runtimeCache = appCaches.RuntimeCache;
		_webHostEnvironment = webHostEnvironment;
		_httpClientFactory = httpClientFactory;
		_logger = logger;

		options.OnChange(config => _options = config);
		_options = options.CurrentValue;
	}

	public async Task<IReadOnlyList<ScriptItem>> GetScriptItemsAsync(CancellationToken cancellationToken)
	{
		Log.LoadingScriptItemsFromDatabase(_logger);

		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<ScriptItem>()
			.OrderBy<ScriptItem>(x => x.Src);

		var items = await scope.Database.FetchAsync<ScriptItem>(sql, cancellationToken);
		scope.Complete();
		return items;
	}

	public async Task<ScriptItem?> GetScriptItemAsync(Guid id, CancellationToken cancellationToken)
	{
		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<ScriptItem>()
			.Where<ScriptItem>(x => x.Id == id);

		var item = await scope.Database.FirstOrDefaultAsync<ScriptItem>(sql, cancellationToken);
		scope.Complete();
		return item;
	}

	public async Task<IReadOnlyCollection<string>> GetCachedScriptHashesAsync(CancellationToken cancellationToken)
	{
		var factoryCalled = false;

		// Same race-safe pattern as CspService.GetCachedCspDefinitionAsync: the Task is inserted
		// into the cache synchronously, before the DB call it wraps has started, so a save/delete
		// that clears this key while a load is in flight can't be resurrected by that load afterwards.
		var load = (Task<IReadOnlyCollection<string>>)_runtimeCache.Get(CacheKey, () =>
		{
			factoryCalled = true;
			return LoadScriptHashesAsync(CancellationToken.None);
		}, timeout: null)!;

		IReadOnlyCollection<string> hashes;

		try
		{
			hashes = await load.WaitAsync(cancellationToken);
		}
		catch (Exception) when (load.IsFaulted)
		{
			_runtimeCache.ClearByKey(CacheKey);
			throw;
		}

		if (!factoryCalled)
		{
			Log.ScriptItemsRetrievedFromCache(_logger);
		}

		return hashes;
	}

	private async Task<IReadOnlyCollection<string>> LoadScriptHashesAsync(CancellationToken cancellationToken)
	{
		var items = await GetScriptItemsAsync(cancellationToken);
		return [.. items.Where(x => !string.IsNullOrWhiteSpace(x.Hash)).Select(x => x.Hash!).Distinct()];
	}

	public async Task<string?> GetHashAsync(string src, CancellationToken cancellationToken)
	{
		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<ScriptItem>()
			.Where<ScriptItem>(x => x.Src == src);

		var item = await scope.Database.FirstOrDefaultAsync<ScriptItem>(sql, cancellationToken);
		scope.Complete();
		return item?.Hash;
	}

	public async Task<ScriptItem> SaveScriptItemAsync(ScriptItem item, CancellationToken cancellationToken)
	{
		if (item.Id == Guid.Empty)
		{
			item.Id = Guid.NewGuid();
		}

		try
		{
			item.Hash = await GenerateHashAsync(item.Src, cancellationToken);
			item.LastUpdated = DateTime.UtcNow;

			using (var scope = _scopeProvider.CreateScope())
			{
				await scope.Database.SaveAsync(item, cancellationToken);
				scope.Complete();
			}

			Log.ScriptItemSaved(_logger, item.Id, item.Src);
		}
		catch (Exception ex)
		{
			Log.ScriptItemSaveFailed(_logger, item.Id, ex);
			throw;
		}
		finally
		{
			_runtimeCache.ClearByKey(CacheKey);
		}

		return item;
	}

	public async Task<ScriptItem> RegenerateHashAsync(Guid id, CancellationToken cancellationToken)
	{
		var item = await GetScriptItemAsync(id, cancellationToken)
			?? throw new InvalidOperationException($"Script item {id} does not exist.");

		item.Hash = await GenerateHashAsync(item.Src, cancellationToken);
		item.LastUpdated = DateTime.UtcNow;

		using (var scope = _scopeProvider.CreateScope())
		{
			await scope.Database.SaveAsync(item, cancellationToken);
			scope.Complete();
		}

		_runtimeCache.ClearByKey(CacheKey);
		Log.ScriptItemHashRegenerated(_logger, item.Id);

		return item;
	}

	public async Task<ScriptItem> SetHashAsync(Guid id, string hash, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(hash))
		{
			throw new ArgumentException("Hash must not be empty.", nameof(hash));
		}

		var item = await GetScriptItemAsync(id, cancellationToken)
			?? throw new InvalidOperationException($"Script item {id} does not exist.");

		item.Hash = hash.Trim();
		item.LastUpdated = DateTime.UtcNow;

		using (var scope = _scopeProvider.CreateScope())
		{
			await scope.Database.SaveAsync(item, cancellationToken);
			scope.Complete();
		}

		_runtimeCache.ClearByKey(CacheKey);
		Log.ScriptItemHashSetManually(_logger, item.Id);

		return item;
	}

	public async Task DeleteScriptItemAsync(Guid id, CancellationToken cancellationToken)
	{
		using (var scope = _scopeProvider.CreateScope())
		{
			var cmdDelete = scope.Database.DeleteManyAsync<ScriptItem>().Where(x => x.Id == id);
			await cmdDelete.Execute(cancellationToken);
			scope.Complete();
		}

		_runtimeCache.ClearByKey(CacheKey);
		Log.ScriptItemDeleted(_logger, id);
	}

	public async Task ResyncScriptItemsAsync(CancellationToken cancellationToken)
	{
		Log.ScriptItemStartupSyncStarted(_logger);

		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<ScriptItem>()
			.Where<ScriptItem>(x => x.SynchroniseOnStartup);

		var items = await scope.Database.FetchAsync<ScriptItem>(sql, cancellationToken);

		foreach (var item in items)
		{
			try
			{
				item.Hash = await GenerateHashAsync(item.Src, cancellationToken);
				item.LastUpdated = DateTime.UtcNow;
				await scope.Database.SaveAsync(item, cancellationToken);
			}
			catch (Exception ex)
			{
				// One script failing to resolve (e.g. transient network issue) shouldn't stop the rest.
				Log.ScriptItemHashGenerationFailed(_logger, item.Id, item.Src, ex);
			}
		}

		scope.Complete();
		_runtimeCache.ClearByKey(CacheKey);
	}

	private async Task<string> GenerateHashAsync(string src, CancellationToken cancellationToken)
	{
		try
		{
			var bytes = await ReadScriptBytesAsync(src, cancellationToken);
			return ComputeHash(bytes, _options.ScriptHashAlgorithm);
		}
		catch (Exception ex)
		{
			Log.ScriptItemHashGenerationFailed(_logger, Guid.Empty, src, ex);
			throw;
		}
	}

	private async Task<byte[]> ReadScriptBytesAsync(string src, CancellationToken cancellationToken)
	{
		// Protocol-relative and absolute URLs are downloaded directly; anything else is treated
		// as a path relative to the site's web root and read straight off disk, so hash generation
		// doesn't need to make a self-referential HTTP call back into the site it's running in.
		var absoluteSrc = src.StartsWith("//", StringComparison.Ordinal) ? $"https:{src}" : src;

		if (Uri.TryCreate(absoluteSrc, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			using var client = _httpClientFactory.CreateClient(nameof(ScriptItemService));
			return await client.GetByteArrayAsync(uri, cancellationToken);
		}

		var fileInfo = _webHostEnvironment.WebRootFileProvider.GetFileInfo(src.TrimStart('~'));
		if (!fileInfo.Exists)
		{
			throw new FileNotFoundException($"Could not resolve script source '{src}' to a local file under the web root.", src);
		}

		using var stream = fileInfo.CreateReadStream();
		using var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream, cancellationToken);
		return memoryStream.ToArray();
	}

	private static string ComputeHash(byte[] input, string algorithm) => algorithm.ToLowerInvariant() switch
	{
		"sha256" => $"sha256-{Convert.ToBase64String(SHA256.HashData(input))}",
		"sha512" => $"sha512-{Convert.ToBase64String(SHA512.HashData(input))}",
		_ => $"sha384-{Convert.ToBase64String(SHA384.HashData(input))}",
	};
}
