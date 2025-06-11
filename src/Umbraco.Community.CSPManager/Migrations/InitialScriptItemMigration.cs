namespace Umbraco.Community.CSPManager.Migrations;

using NPoco;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Community.CSPManager.Models;

public sealed class InitialScriptItemMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-scriptitem-init";
	private readonly IUmbracoVersion UmbracoVersion;

	public InitialScriptItemMigration(IMigrationContext context, IUmbracoVersion umbracoVersion) : base(context)
	{
		UmbracoVersion = umbracoVersion;
	}

	protected override void Migrate()
	{
		if (!TableExists(nameof(ScriptItem)))
		{
			Create.Table<ScriptItem>().Do();
		}
	}
}
