import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { ScriptItems, type CspApiScriptItem } from '../api';

export class UmbCspScriptItemRepository extends UmbRepositoryBase {
	constructor(host: UmbControllerHost) {
		super(host);
	}

	async getAll() {
		const { data, error } = await tryExecute(this, ScriptItems.getUmbracoCspApiV1ScriptItems(), {
			disableNotifications: false,
		});

		if (data) {
			return { data };
		}

		return { error };
	}

	async save(scriptItem: CspApiScriptItem) {
		const { data, error } = await tryExecute(
			this,
			ScriptItems.postUmbracoCspApiV1ScriptItemsSave({
				body: scriptItem,
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	async regenerateHash(id: string) {
		const { data, error } = await tryExecute(
			this,
			ScriptItems.postUmbracoCspApiV1ScriptItemsByIdRegenerateHash({
				path: { id },
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	async setHash(id: string, hash: string) {
		const { data, error } = await tryExecute(
			this,
			ScriptItems.postUmbracoCspApiV1ScriptItemsByIdHash({
				path: { id },
				body: { hash },
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	async delete(id: string) {
		const { error } = await tryExecute(
			this,
			ScriptItems.deleteUmbracoCspApiV1ScriptItemsById({
				path: { id },
			}),
			{ disableNotifications: false }
		);

		return { error };
	}
}

export { UmbCspScriptItemRepository as api };
