import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UmbNotificationContext } from '@umbraco-cms/backoffice/notification';
import { UMB_CONFIRM_MODAL, umbOpenModal } from '@umbraco-cms/backoffice/modal';
import type { CspApiScriptItem } from '@/api';
import { UmbCspScriptItemRepository } from '../../../repository/csp-script-item.repository.js';

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

@customElement('umb-csp-scripts-view')
export class UmbCspScriptsViewElement extends UmbLitElement {
	#repository: UmbCspScriptItemRepository;
	#notificationContext?: UmbNotificationContext;

	@state()
	private _items: Array<CspApiScriptItem> = [];

	@state()
	private _loading = true;

	@state()
	private _busyIds = new Set<string>();

	@state()
	private _newSrc = '';

	@state()
	private _newDescription = '';

	@state()
	private _newSync = false;

	@state()
	private _adding = false;

	constructor() {
		super();

		this.#repository = new UmbCspScriptItemRepository(this);

		this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
			this.#notificationContext = context;
		});

		this.#load();
	}

	async #load() {
		this._loading = true;
		const { data, error } = await this.#repository.getAll();
		if (data) {
			this._items = data;
		} else if (error) {
			this.#notify('danger', 'Failed to load scripts', error.message);
		}
		this._loading = false;
	}

	#notify(type: 'positive' | 'danger', headline: string, message?: string) {
		this.#notificationContext?.peek(type, { data: { headline, message: message ?? '' } });
	}

	async #handleAdd() {
		if (!this._newSrc.trim() || this._adding) return;

		this._adding = true;

		const { data, error } = await this.#repository.save({
			id: EMPTY_GUID,
			src: this._newSrc.trim(),
			description: this._newDescription.trim() || null,
			hash: null,
			synchroniseOnStartup: this._newSync,
			lastUpdated: null,
		});

		if (data) {
			this._newSrc = '';
			this._newDescription = '';
			this._newSync = false;
			this.#notify('positive', 'Script added', `A hash was generated for ${data.src}.`);
			await this.#load();
		} else if (error) {
			this.#notify('danger', 'Failed to add script', error.message);
		}

		this._adding = false;
	}

	async #handleToggleSync(item: CspApiScriptItem, checked: boolean) {
		this._busyIds = new Set(this._busyIds).add(item.id);

		const { data, error } = await this.#repository.save({ ...item, synchroniseOnStartup: checked });

		if (data) {
			this._items = this._items.map((i) => (i.id === data.id ? data : i));
		} else if (error) {
			this.#notify('danger', 'Failed to update script', error.message);
		}

		this.#clearBusy(item.id);
	}

	async #handleRegenerate(item: CspApiScriptItem) {
		this._busyIds = new Set(this._busyIds).add(item.id);

		const { data, error } = await this.#repository.regenerateHash(item.id);

		if (data) {
			this._items = this._items.map((i) => (i.id === data.id ? data : i));
			this.#notify('positive', 'Hash regenerated', `New hash generated for ${data.src}.`);
		} else if (error) {
			this.#notify('danger', 'Failed to regenerate hash', error.message);
		}

		this.#clearBusy(item.id);
	}

	async #handleDelete(item: CspApiScriptItem) {
		try {
			await umbOpenModal(this, UMB_CONFIRM_MODAL, {
				data: {
					headline: 'Delete script',
					content: `Are you sure you want to remove ${item.src} from CSP Manager? Its hash will no longer be added to script-src.`,
					color: 'danger',
					confirmLabel: 'Delete',
				},
			});
		} catch {
			return;
		}

		this._busyIds = new Set(this._busyIds).add(item.id);

		const { error } = await this.#repository.delete(item.id);

		if (!error) {
			this._items = this._items.filter((i) => i.id !== item.id);
			this.#notify('positive', 'Script deleted');
		} else {
			this.#notify('danger', 'Failed to delete script', error.message);
		}

		this.#clearBusy(item.id);
	}

	#clearBusy(id: string) {
		const next = new Set(this._busyIds);
		next.delete(id);
		this._busyIds = next;
	}

	render() {
		return html`
			<uui-box headline="Scripts">
				<p class="intro">
					Scripts registered here have a Subresource Integrity hash generated automatically. Add
					<code>csp-manager-add-hash</code> to a <code>&lt;script&gt;</code> tag with a matching <code>src</code> to
					stamp its <code>integrity</code> attribute and fold the hash into the <code>script-src</code> directive.
				</p>

				${this._loading ? html`<uui-loader></uui-loader>` : this.#renderTable()}
			</uui-box>

			<uui-box headline="Add script">
				<div class="add-form">
					<uui-form-layout-item>
						<uui-label slot="label">Src</uui-label>
						<span slot="description">An absolute URL, protocol-relative URL, or a path relative to the web root</span>
						<uui-input
							label="Src"
							placeholder="/scripts/site.js"
							.value=${this._newSrc}
							@input=${(e: Event) => (this._newSrc = (e.target as HTMLInputElement).value)}>
						</uui-input>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label slot="label">Description</uui-label>
						<uui-input
							label="Description"
							.value=${this._newDescription}
							@input=${(e: Event) => (this._newDescription = (e.target as HTMLInputElement).value)}>
						</uui-input>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label slot="label">Synchronise on startup</uui-label>
						<span slot="description">Automatically regenerate this script's hash every time Umbraco starts</span>
						<uui-toggle
							label="Synchronise on startup"
							.checked=${this._newSync}
							@change=${(e: Event) => (this._newSync = (e.target as HTMLInputElement).checked)}>
						</uui-toggle>
					</uui-form-layout-item>

					<uui-button
						label="Add script"
						look="primary"
						color="positive"
						.disabled=${!this._newSrc.trim() || this._adding}
						@click=${this.#handleAdd}>
						${this._adding ? 'Adding…' : 'Add script'}
					</uui-button>
				</div>
			</uui-box>
		`;
	}

	#renderTable() {
		if (this._items.length === 0) {
			return html`<div class="empty">No scripts registered yet.</div>`;
		}

		return html`
			<uui-table>
				<uui-table-head>
					<uui-table-head-cell>Src</uui-table-head-cell>
					<uui-table-head-cell>Description</uui-table-head-cell>
					<uui-table-head-cell>Hash</uui-table-head-cell>
					<uui-table-head-cell>Sync on startup</uui-table-head-cell>
					<uui-table-head-cell></uui-table-head-cell>
				</uui-table-head>
				${this._items.map((item) => this.#renderRow(item))}
			</uui-table>
		`;
	}

	#renderRow(item: CspApiScriptItem) {
		const busy = this._busyIds.has(item.id);

		return html`
			<uui-table-row>
				<uui-table-cell><span class="src">${item.src}</span></uui-table-cell>
				<uui-table-cell>${item.description || ''}</uui-table-cell>
				<uui-table-cell>
					${item.hash
						? html`<code class="hash" title=${item.hash}>${item.hash}</code>`
						: html`<span class="no-hash">Not generated</span>`}
				</uui-table-cell>
				<uui-table-cell>
					<uui-toggle
						label="Synchronise on startup"
						.checked=${item.synchroniseOnStartup}
						.disabled=${busy}
						@change=${(e: Event) => this.#handleToggleSync(item, (e.target as HTMLInputElement).checked)}>
					</uui-toggle>
				</uui-table-cell>
				<uui-table-cell>
					<div class="row-actions">
						<uui-button
							label="Regenerate hash"
							compact
							.disabled=${busy}
							@click=${() => this.#handleRegenerate(item)}>
							<uui-icon name="icon-sync"></uui-icon>
						</uui-button>
						<uui-button label="Delete" compact color="danger" .disabled=${busy} @click=${() => this.#handleDelete(item)}>
							<uui-icon name="icon-trash"></uui-icon>
						</uui-button>
					</div>
				</uui-table-cell>
			</uui-table-row>
		`;
	}

	static styles = [
		css`
			:host {
				display: block;
				padding: var(--uui-size-layout-1);
			}

			uui-box + uui-box {
				margin-top: var(--uui-size-space-6);
			}

			.intro {
				margin-top: 0;
				color: var(--uui-color-text-alt);
			}

			.intro code {
				font-family: var(--uui-font-family-monospace);
				background-color: var(--uui-color-surface-alt);
				padding: 0 var(--uui-size-space-1);
				border-radius: var(--uui-border-radius);
			}

			.empty {
				color: var(--uui-color-text-alt);
				padding: var(--uui-size-space-4) 0;
			}

			.src {
				font-family: var(--uui-font-family-monospace);
			}

			.hash {
				display: inline-block;
				max-width: 220px;
				overflow: hidden;
				text-overflow: ellipsis;
				white-space: nowrap;
				vertical-align: bottom;
				font-family: var(--uui-font-family-monospace);
				font-size: 0.85em;
			}

			.no-hash {
				color: var(--uui-color-text-alt);
				font-style: italic;
			}

			.row-actions {
				display: flex;
				gap: var(--uui-size-space-2);
			}

			.add-form {
				display: grid;
				gap: var(--uui-size-space-5);
				max-width: 480px;
			}
		`,
	];
}

export default UmbCspScriptsViewElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-scripts-view': UmbCspScriptsViewElement;
	}
}
