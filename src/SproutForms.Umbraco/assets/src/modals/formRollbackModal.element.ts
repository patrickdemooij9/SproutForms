import {
  css,
  customElement,
  html,
  nothing,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { diffWords } from "@umbraco-cms/backoffice/utils";
import {
  FormChangeBackofficeModel,
  FormChangeSection,
  FormChangeType,
  FormPropertyChangeBackofficeModel,
  FormVersionBackofficeModel,
  FormVersionComparisonBackofficeModel,
} from "../api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import {
  FormRollbackModalData,
  FormRollbackModalValue,
} from "./formRollbackModal.token";

const DATE_OPTIONS: Intl.DateTimeFormatOptions = {
  day: "numeric",
  month: "long",
  hour: "numeric",
  minute: "2-digit",
};

const SECTION_HEADLINES: Record<FormChangeSection, string> = {
  [FormChangeSection.FORM]: "Settings",
  [FormChangeSection.PAGE]: "Pages",
  [FormChangeSection.FIELD]: "Fields",
  [FormChangeSection.WORKFLOW]: "Workflows",
  [FormChangeSection.OUTCOME]: "After submitting",
};

const CHANGE_TAGS: Record<FormChangeType, { label: string; color: string }> = {
  [FormChangeType.ADDED]: { label: "Added", color: "positive" },
  [FormChangeType.REMOVED]: { label: "Removed", color: "danger" },
  [FormChangeType.CHANGED]: { label: "Changed", color: "default" },
};

@customElement("sf-form-rollback-modal")
export default class FormRollbackModalElement extends UmbModalBaseElement<
  FormRollbackModalData,
  FormRollbackModalValue
> {
  #source = new SproutFormsSource(this);

  @state()
  private _versions?: Array<FormVersionBackofficeModel>;

  @state()
  private _comparison?: FormVersionComparisonBackofficeModel;

  @state()
  private _rollingBack = false;

  override connectedCallback() {
    super.connectedCallback();
    this.#loadVersions();
  }

  // Like Umbraco, the current version isn't offered: rolling back to it would change nothing
  async #loadVersions() {
    const { data } = await this.#source.getFormVersions(this.data!.formId);
    this._versions = (data ?? []).filter((version) => !version.isCurrent);
    if (this._versions.length > 0) {
      this.#selectVersion(this._versions[0].id);
    }
  }

  async #selectVersion(versionId: string) {
    const { data } = await this.#source.compareFormVersion(this.data!.formId, versionId);
    this._comparison = data;
  }

  async #rollback() {
    if (!this._comparison) return;

    this._rollingBack = true;
    const { data } = await this.#source.rollbackForm(this.data!.formId, this._comparison.version.id);
    this._rollingBack = false;
    // tryExecute has already shown the error
    if (!data) return;

    this.updateValue({ form: data });
    this._submitModal();
  }

  #renderVersions() {
    if (!this._versions) return html`<uui-box headline="Versions"><uui-loader></uui-loader></uui-box>`;
    if (this._versions.length === 0) {
      return html`<uui-box headline="Versions">This form has no earlier versions.</uui-box>`;
    }

    return html`
      <uui-box id="versions-box" headline="Versions">
        ${repeat(
          this._versions,
          (version) => version.id,
          (version) => html`
            <button
              class="rollback-item ${this._comparison?.version.id === version.id ? "active" : ""}"
              @click=${() => this.#selectVersion(version.id)}
            >
              <p class="rollback-item-date">
                <umb-localize-date .date=${version.createdAt} .options=${DATE_OPTIONS}></umb-localize-date>
              </p>
              <p>${version.createdByName}</p>
              <p>Version ${version.version}</p>
            </button>
          `,
        )}
      </uui-box>
    `;
  }

  #renderComparison() {
    const comparison = this._comparison;
    if (!comparison) {
      return html`<uui-box id="box-right" class="empty">No selected version</uui-box>`;
    }

    const headline = `${this.localize.date(comparison.version.createdAt, DATE_OPTIONS)} - ${comparison.version.createdByName}`;
    return html`
      <uui-box id="box-right" headline=${headline}>
        ${this.#renderNotices(comparison)}
        ${when(
          comparison.changes.length > 0,
          () => html`
            <p>
              These are the changes rolling back to version ${comparison.version.version} makes to the current form.
              <span class="diff-removed">Red text</span> is removed, <span class="diff-added">green text</span> is added.
            </p>
            ${this.#renderChanges(comparison.changes)}
          `,
        )}
      </uui-box>
    `;
  }

  #renderNotices(comparison: FormVersionComparisonBackofficeModel) {
    return html`
      ${when(
        comparison.rollbackErrors.length > 0,
        () => html`
          <div class="notice danger">
            <strong>This version can't be rolled back to:</strong>
            <ul>${comparison.rollbackErrors.map((error) => html`<li>${error}</li>`)}</ul>
          </div>
        `,
      )}
      ${when(
        comparison.removedFieldsWithSubmissions.length > 0,
        () => html`
          <div class="notice warning">
            <strong>Existing submissions have values for fields this version doesn't have:</strong>
            ${comparison.removedFieldsWithSubmissions.join(", ")}. The submissions keep those values.
          </div>
        `,
      )}
    `;
  }

  #renderChanges(changes: Array<FormChangeBackofficeModel>) {
    const sections = Object.keys(SECTION_HEADLINES) as Array<FormChangeSection>;
    return sections.map((section) => {
      const sectionChanges = changes.filter((change) => change.section === section);
      if (sectionChanges.length === 0) return nothing;

      return html`
        <h4>${SECTION_HEADLINES[section]}</h4>
        ${sectionChanges.map((change) => this.#renderChange(change))}
      `;
    });
  }

  #renderChange(change: FormChangeBackofficeModel) {
    const tag = CHANGE_TAGS[change.changeType];
    return html`
      <div class="change">
        <div class="change-header">
          <strong>${change.name}</strong>
          <uui-tag look="secondary" color=${tag.color}>${tag.label}</uui-tag>
        </div>
        <uui-table>
          <uui-table-column style="width: 0"></uui-table-column>
          <uui-table-column></uui-table-column>
          <uui-table-head>
            <uui-table-head-cell>Property</uui-table-head-cell>
            <uui-table-head-cell>Value</uui-table-head-cell>
          </uui-table-head>
          ${repeat(
            change.properties,
            (property) => property.name,
            (property) => html`
              <uui-table-row>
                <uui-table-cell>${property.name}</uui-table-cell>
                <uui-table-cell>${this.#renderDiff(property)}</uui-table-cell>
              </uui-table-row>
            `,
          )}
        </uui-table>
      </div>
    `;
  }

  #renderDiff(property: FormPropertyChangeBackofficeModel) {
    return diffWords(property.currentValue ?? "", property.versionValue ?? "").map((part) =>
      part.added
        ? html`<span class="diff-added">${part.value}</span>`
        : part.removed
          ? html`<span class="diff-removed">${part.value}</span>`
          : part.value,
    );
  }

  override render() {
    return html`
      <umb-body-layout headline="Rollback">
        <div id="main">
          <div id="box-left">${this.#renderVersions()}</div>
          ${this.#renderComparison()}
        </div>
        <umb-footer-layout slot="footer">
          <uui-button
            slot="actions"
            look="secondary"
            label="Cancel"
            @click=${() => this._rejectModal()}
          ></uui-button>
          <uui-button
            slot="actions"
            look="primary"
            label="Rollback"
            .state=${this._rollingBack ? "waiting" : undefined}
            ?disabled=${!this._comparison ||
            this._comparison.rollbackErrors.length > 0 ||
            this._rollingBack}
            @click=${() => this.#rollback()}
          ></uui-button>
        </umb-footer-layout>
      </umb-body-layout>
    `;
  }

  static override styles = [
    UmbTextStyles,
    css`
      :host {
        color: var(--uui-color-text);
      }

      #main {
        display: flex;
        gap: var(--uui-size-space-5);
        width: 100%;
        height: 100%;
      }

      #box-left {
        max-width: 500px;
        flex: 1;
        overflow: auto;
        height: 100%;
      }

      #box-right {
        flex: 2;
        overflow: auto;
        height: 100%;
      }

      #box-right.empty {
        display: flex;
        align-items: center;
        justify-content: center;
      }

      #versions-box {
        --uui-box-default-padding: 0;
      }

      .rollback-item {
        position: relative;
        display: block;
        width: 100%;
        padding: var(--uui-size-space-5);
        border: none;
        background: none;
        color: inherit;
        font: inherit;
        text-align: left;
        cursor: pointer;
      }

      .rollback-item::after {
        content: "";
        position: absolute;
        inset: 2px;
        display: block;
        border: 2px solid transparent;
        pointer-events: none;
      }

      .rollback-item.active::after,
      .rollback-item:hover::after {
        border-color: var(--uui-color-selected);
      }

      .rollback-item:not(.active):hover::after {
        opacity: 0.5;
      }

      .rollback-item p {
        margin: 0;
        opacity: 0.5;
      }

      p.rollback-item-date {
        opacity: 1;
      }

      h4 {
        margin: var(--uui-size-space-6) 0 var(--uui-size-space-3);
      }

      .change + .change {
        margin-top: var(--uui-size-space-4);
      }

      .change-header {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-3);
        margin-bottom: var(--uui-size-space-2);
      }

      .notice {
        padding: var(--uui-size-space-4);
        margin-bottom: var(--uui-size-space-4);
        border-radius: var(--uui-border-radius);
      }

      .notice ul {
        margin: var(--uui-size-space-2) 0 0;
      }

      .notice.danger {
        background-color: var(--uui-color-danger);
        color: var(--uui-color-danger-contrast);
      }

      .notice.warning {
        background-color: var(--uui-color-warning);
        color: var(--uui-color-warning-contrast);
      }

      uui-table {
        --uui-table-cell-padding: var(--uui-size-space-1) var(--uui-size-space-4);
      }

      uui-table-head-cell {
        background-color: var(--uui-color-surface-alt);
      }

      uui-table-head-cell,
      uui-table-cell {
        border-top: 1px solid var(--uui-color-border);
        border-left: 1px solid var(--uui-color-border);
      }

      uui-table-head-cell:last-child,
      uui-table-cell:last-child {
        border-right: 1px solid var(--uui-color-border);
        word-break: break-word;
      }

      uui-table-cell:first-child {
        white-space: nowrap;
      }

      uui-table-row:last-child uui-table-cell {
        border-bottom: 1px solid var(--uui-color-border);
      }

      .diff-added {
        background-color: #00c43e63;
      }

      .diff-removed {
        background-color: #ff35356a;
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-form-rollback-modal": FormRollbackModalElement;
  }
}
