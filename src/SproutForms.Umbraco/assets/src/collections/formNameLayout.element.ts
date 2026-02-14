import { customElement, property, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

@customElement("sf-form-name-column-layout")
export class FormNameLayout extends UmbLitElement {
	@property({ attribute: false })
	value?: { unique: string; name: string, entityType: string };

	override render() {
		if (!this.value) return nothing;

		const entity = this.value.entityType === "sf-form" ? "sprout-form" : "sprout-folder";

		return html`<a href="/umbraco/section/sproutForms/workspace/${entity}/edit/${this.value.unique}">${this.value.name}</a>`;
	}

	static override styles = [
		css`
			:host {
				white-space: nowrap;
			}

            a {
                color: var(--uui-color-interactive);
            }

            a:hover{
                color: var(--uui-color-interactive-emphasis);
            }
		`,
	];
}

declare global {
	interface HTMLElementTagNameMap {
		'sf-form-name-column-layout': FormNameLayout;
	}
}