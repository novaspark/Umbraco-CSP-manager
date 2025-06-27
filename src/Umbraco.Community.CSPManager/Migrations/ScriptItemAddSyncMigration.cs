namespace Umbraco.Community.CSPManager.Migrations;

using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

public class ScriptItemAddSyncMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-add-script-sync-property";

	public ScriptItemAddSyncMigration(IMigrationContext context) : base(context)
	{
	}

	protected override void Migrate()
	{
		if (!ColumnExists(nameof(ScriptItem), nameof(SchemaUpdates.SynchroniseOnStartup)))
		{
			Create.Column(nameof(SchemaUpdates.SynchroniseOnStartup))
			.OnTable(nameof(ScriptItem))
			.AsBoolean().Nullable().Do();
		}
	}

	public class SchemaUpdates
	{
		public bool? SynchroniseOnStartup { get; set; }
	}
}
