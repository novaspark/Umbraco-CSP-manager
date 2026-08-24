using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

/// <summary>
/// Bridges a database that was migrated by the pre-rewrite (net6/7/8, AngularJS-backoffice) build of
/// this package - the last state it ever reached was <see cref="LegacyFinalStateKey"/> - onto this
/// plan's current final state.
/// </summary>
/// <remarks>
/// That old build's final migration chain already created everything this plan's chain also creates
/// (CspDefinition/CspDefinitionSource with ReportingDirective/ReportUri/UpgradeInsecureRequests/
/// ExcludePaths, and a ScriptItem table with a SynchroniseOnStartup column), so nothing needs
/// (re)creating here. The one real difference: that column was added there as nullable
/// (<c>BIT NULL</c>), while <see cref="ScriptItem.SynchroniseOnStartup"/> is a non-nullable <c>bool</c>
/// here, so any pre-existing NULL values are normalised to 0 to avoid a cast failure the first time
/// they're read back.
/// </remarks>
public sealed class LegacyForkUpgradeMigration : AsyncMigrationBase
{
	public const string LegacyFinalStateKey = "csp-manager-add-exclude-paths-property";

	public LegacyForkUpgradeMigration(IMigrationContext context) : base(context)
	{
	}

	protected override Task MigrateAsync()
	{
		if (TableExists(nameof(ScriptItem)) && ColumnExists(nameof(ScriptItem), nameof(ScriptItem.SynchroniseOnStartup)))
		{
			Database.Execute(
				$"UPDATE {SqlSyntax.GetQuotedTableName(nameof(ScriptItem))} " +
				$"SET {SqlSyntax.GetQuotedColumnName(nameof(ScriptItem.SynchroniseOnStartup))} = 0 " +
				$"WHERE {SqlSyntax.GetQuotedColumnName(nameof(ScriptItem.SynchroniseOnStartup))} IS NULL");
		}

		return Task.CompletedTask;
	}
}
