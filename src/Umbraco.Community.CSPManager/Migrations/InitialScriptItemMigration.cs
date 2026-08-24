using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

public sealed class InitialScriptItemMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-init-script-items";

	public InitialScriptItemMigration(IMigrationContext context) : base(context)
	{
	}

	protected override Task MigrateAsync()
	{
		if (!TableExists(nameof(ScriptItem)))
		{
			Create.Table<ScriptItem>().Do();
		}

		return Task.CompletedTask;
	}
}
