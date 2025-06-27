namespace Umbraco.Community.CSPManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Community.CSPManager.Models;

public interface IScriptItemService
{
	Task<string?> GetHash(string scriptSrc);
	Task<Dictionary<string, string>> GetCachedScriptItemsDictionary();
	Task<IList<ScriptItem>> FindAllScriptItems();
	Task<ScriptItem> Add(ScriptItem item);
	Task<IList<ScriptItem>> GetSavedScriptItems();
	Task<ScriptItem> Update(Guid id, string description, bool? synchroniseOnStartup);
	Task Delete(Guid id);
	Task ReSyncScriptItems();
}
