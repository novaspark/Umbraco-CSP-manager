using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

public sealed class DefinitionAddExcludePathsMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-add-exclude-paths";

	public DefinitionAddExcludePathsMigration(IMigrationContext context) : base(context)
	{
	}

	protected override Task MigrateAsync()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.ExcludePaths)))
		{
			Create.Column(nameof(SchemaUpdates.ExcludePaths))
			.OnTable(nameof(CspDefinition))
			.AsString().Nullable().Do();
		}

		return Task.CompletedTask;
	}

	[ExcludeFromCodeCoverage(Justification = "Migration model so not accessed directly.")]
	public sealed class SchemaUpdates
	{
		public string? ExcludePaths { get; set; }
	}
}
