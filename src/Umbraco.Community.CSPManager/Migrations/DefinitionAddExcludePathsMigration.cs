namespace Umbraco.Community.CSPManager.Migrations;

using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

public class DefinitionAddExcludePathsMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-add-exclude-paths-property";

	public DefinitionAddExcludePathsMigration(IMigrationContext context) : base(context)
	{
	}

	protected override void Migrate()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.ExcludePaths)))
		{
			Create.Column(nameof(SchemaUpdates.ExcludePaths))
			.OnTable(nameof(CspDefinition))
			.AsString().Nullable().Do();
		}
	}

	public class SchemaUpdates
	{
		public string? ExcludePaths { get; set; }
	}
}
