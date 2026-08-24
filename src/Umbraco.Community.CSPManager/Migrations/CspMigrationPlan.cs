using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Community.CSPManager.Migrations;

public sealed class CspMigrationPlan : PackageMigrationPlan
{
	public CspMigrationPlan() : base(Constants.PackageAlias)
	{
	}

	protected override void DefinePlan()
	{
		To<InitialCspManagerMigration>(InitialCspManagerMigration.MigrationKey);
		To<AddCspManagerSectionToAdminUserGroupMigration>(AddCspManagerSectionToAdminUserGroupMigration.MigrationKey);
		To<ReportingMigration>(ReportingMigration.MigrationKey);
		To<MaxSourceLengthMigration>(MaxSourceLengthMigration.MigrationKey);
		To<UpgradeInsecureRequestsMigration>(UpgradeInsecureRequestsMigration.MigrationKey);
		To<InitialScriptItemMigration>(InitialScriptItemMigration.MigrationKey);
		To<DefinitionAddExcludePathsMigration>(DefinitionAddExcludePathsMigration.MigrationKey);

		// Bridges a database migrated by the pre-rewrite (net6/7/8) build of this package - its
		// migration chain's final state was this literal key, which doesn't exist anywhere in the
		// chain above. See LegacyForkUpgradeMigration for why nothing but a data-quality fixup is
		// needed to land it on the same final state as a fresh install.
		From(LegacyForkUpgradeMigration.LegacyFinalStateKey);
		To<LegacyForkUpgradeMigration>(DefinitionAddExcludePathsMigration.MigrationKey);
	}
}