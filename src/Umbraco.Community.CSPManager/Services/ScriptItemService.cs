namespace Umbraco.Community.CSPManager.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants.HttpContext;

public class ScriptItemService : IScriptItemService
{
	private readonly ITemplateRepository _templateRepository;
	private readonly IScopeProvider _scopeProvider;
	private readonly IAppPolicyCache _runtimeCache;
	private readonly CspManagerSettings _settings;

	const string _CACHE_KEY = "CspManagerScriptItems";

	public ScriptItemService(IContentService contentTypeService,
		IUmbracoContextFactory umbracoContextFactory, IServiceProvider serviceProvider,
		IScopeProvider scopeProvider, ITemplateRepository templateRepository,
		AppCaches appCaches,IOptions<CspManagerSettings> options)
	{
		_scopeProvider = scopeProvider;
		_templateRepository = templateRepository;
		_runtimeCache = appCaches.RuntimeCache;
		_settings = options.Value;
	}

	public async Task<string?> GetHash(string scriptSrc)
	{
		var scriptItems = await GetCachedScriptItemsDictionary();
		return scriptItems.TryGetValue(scriptSrc, out var hashValue) ? hashValue : null;
	}

	public async Task<Dictionary<string, string>> GetCachedScriptItemsDictionary()
	{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		return await _runtimeCache.GetCacheItem(_CACHE_KEY, async () => await GetScriptItemsDictionary());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
	}

	private async Task<Dictionary<string, string>> GetScriptItemsDictionary()
	{
		var d = new Dictionary<string, string>();
		var allItems = await GetSavedScriptItems();
		if (allItems != null)
		{
			foreach (var item in allItems)
			{
				if (item.Src!=null && !d.ContainsKey(item.Src))
				{
					d.Add(item.Src, item.Hash ?? "NOTSET");
				}
			}
		}

		return d;
	}

	public async Task<IList<ScriptItem>> FindAllScriptItems()
	{
		var list = new List<ScriptItem>();
		var savedScripts = await GetSavedScriptItems();

		using (_scopeProvider.CreateCoreScope(autoComplete: true))
		{
			var allTemplates = _templateRepository.GetAll();
			Regex rx = new ("<script.*?src=\"(.*?)\"");

			foreach (var item in allTemplates)
			{
				if (item.Content!=null)
				{
					var matches = rx.Matches(item.Content);
					foreach (Match match in matches)
					{
						if (match.Groups.Count > 1)
						{
							var src = match.Groups[1].Value;
							if (!string.IsNullOrWhiteSpace(src))
							{
								var ss = savedScripts.FirstOrDefault(x=>x.Src == src && x.FileLocation == item.VirtualPath);
								if (ss == null)
								{
									var scriptItem = new ScriptItem
									{
										Id = Guid.NewGuid(),
										Src = src,
										Description = $"Script from {item.Name} template",
										FileLocation = item.VirtualPath,
										Position = match.Index,
										LastUpdated = DateTime.UtcNow
									};
									list.Add(scriptItem);
								}
							}
						}
					}
				}
			}
		}

		return list;
	}

	public async Task<IList<ScriptItem>> GetSavedScriptItems()
	{
		using var scope = _scopeProvider.CreateScope();
		return await scope.Database.FetchAsync<ScriptItem>();
	}

	public async Task ReSyncScriptItems()
	{
		using var scope = _scopeProvider.CreateScope();
		var allItems = await scope.Database.FetchAsync<ScriptItem>();
		
		foreach (var item in allItems.Where(x=>x.SynchroniseOnStartup.GetValueOrDefault()))
		{
			item.Hash = await GenerateHash(item.Src, _settings.SiteUrl);
			item.LastUpdated = DateTime.UtcNow;
			await scope.Database.SaveAsync(item);
		}

		scope.Complete();

		_runtimeCache.ClearByKey(_CACHE_KEY);
	}

	public async Task<ScriptItem> Add(ScriptItem item)
	{
		item.Hash = await GenerateHash(item.Src, _settings.SiteUrl);

		using var scope = _scopeProvider.CreateScope();
		await scope.Database.SaveAsync(item);

		scope.Complete();

		_runtimeCache.ClearByKey(_CACHE_KEY);
		return item;
	}

	public async Task<ScriptItem> Update(Guid id, string description, bool? synchroniseOnStartup)
	{
		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<ScriptItem>()
			.Where<ScriptItem>(x => x.Id == id);

		var si = await scope.Database.FetchAsync<ScriptItem>(sql);
		var scriptItem = si.FirstOrDefault();

		if (scriptItem!=null)
		{
			scriptItem.Hash = await GenerateHash(scriptItem.Src, _settings.SiteUrl);
			scriptItem.LastUpdated = DateTime.Now;
			scriptItem.Description = description;
			scriptItem.SynchroniseOnStartup = synchroniseOnStartup;

			await scope.Database.SaveAsync(scriptItem);

			scope.Complete();
			
			_runtimeCache.ClearByKey(_CACHE_KEY);
			
			return scriptItem;
		}		

		return null;	
	}

	public async Task<ScriptItem> UpdateHash(Guid id, string hash)
	{
		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<ScriptItem>()
			.Where<ScriptItem>(x => x.Id == id);

		var si = await scope.Database.FetchAsync<ScriptItem>(sql);
		var scriptItem = si.FirstOrDefault();

		if (scriptItem != null)
		{
			scriptItem.Hash = hash;
			scriptItem.LastUpdated = DateTime.Now;

			await scope.Database.SaveAsync(scriptItem);

			scope.Complete();

			_runtimeCache.ClearByKey(_CACHE_KEY);

			return scriptItem;
		}

		return null;
	}

	public async Task Delete(Guid id)
	{
		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<ScriptItem>()
			.Where<ScriptItem>(x => x.Id == id);

		var si = await scope.Database.FetchAsync<ScriptItem>(sql);
		var scriptItem = si.FirstOrDefault();

		if (scriptItem != null)
		{
			await scope.Database.DeleteAsync(scriptItem);
			scope.Complete();
		}
	}

	private async Task<string> GenerateHash(string src, string siteRoot)
	{
		// Download contents of script file and generate hash
		// Is it a full path?
		if (src.StartsWith("//"))
		{
			src = "https:" + src; // Handle protocol-relative URLs
		}

		if (!src.StartsWith("http"))
		{
			src = siteRoot.TrimEnd('/') + "/" + src.TrimStart('/');
		}

		using (var httpClient = new HttpClient())
		{
			var result = await httpClient.GetByteArrayAsync(src);
			switch (_settings.HashAlgorithm.ToLowerInvariant())
			{
				case "sha384":
					return Sha384(result);
				case "sha512":
					return Sha512(result);
				default:
					// Default to sha256
					break;
			}
			return Sha256(result);
		}
	}

	private static string Sha256(byte[] input)
	{
		byte[] crypto = SHA256.HashData(input);
		return $"sha256-{Convert.ToBase64String(crypto)}";
	}

	private static string Sha384(byte[] input)
	{
		byte[] crypto = SHA384.HashData(input);
		return $"sha384-{Convert.ToBase64String(crypto)}";
	}

	private static string Sha512(byte[] input)
	{
		byte[] crypto = SHA512.HashData(input);
		return $"sha512-{Convert.ToBase64String(crypto)}";
	}
}
