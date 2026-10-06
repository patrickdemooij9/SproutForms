import { customElement, property, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { getFormSubmissionsPath } from "../workspaces/formSubmissionsWorkspaceContext";

// Opens the form's submissions page; a folder has no value
@customElement("sf-form-submissions-column-layout")
export class FormSubmissionsLayout extends UmbLitElement {
	@property({ attribute: false })
	value?: { unique: string; total: number };

	override render() {
		if (!this.value) return nothing;

		return html`
			<a href=${getFormSubmissionsPath(this.value.unique)} title="View the submissions">
				<umb-icon name="icon-inbox"></umb-icon>
				<span>${this.value.total}</span>
			</a>
		`;
	}

	static override styles = [
		css`
			:host {
				white-space: nowrap;
			}

            a {
                display: inline-flex;
                align-items: center;
                gap: var(--uui-size-space-2);
                color: var(--uui-color-interactive);
            }

            a:hover {
                color: var(--uui-color-interactive-emphasis);
            }
		`,
	];
}

declare global {
	interface HTMLElementTagNameMap {
		'sf-form-submissions-column-layout': FormSubmissionsLayout;
	}
}
