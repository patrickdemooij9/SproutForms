import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  nothing,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { FormDefinitionDto, FormPageDto } from "../models";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { ConditionChangeEvent } from "./fieldEditors/conditionEditor.element";
import "./fieldEditors/conditionEditor.element";

/**
 * The settings of the page the Build tab shows: its title, button labels and when it's shown.
 */
@customElement("sf-page-settings")
export class FormPageSettingsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private definition?: FormDefinitionDto;

  @state()
  private pageIndex = 0;

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;
      this.observe(context?.form, (form) => {
        this.definition = form?.definition;
      });
      this.observe(context?.currentPageIndex, (index) => {
        this.pageIndex = index ?? 0;
      });
    });
  }

  get #page(): FormPageDto | undefined {
    return this.definition?.pages[this.pageIndex];
  }

  #update(changes: Partial<FormPageDto>) {
    this.context?.updatePage(this.pageIndex, changes);
  }

  #onTextChange(property: "title" | "nextLabel" | "previousLabel", event: Event) {
    // Empty means the default, so it isn't stored as an empty string
    const value = (event.target as HTMLInputElement).value;
    this.#update({ [property]: value || null });
  }

  async #removePage() {
    const page = this.#page;
    if (!page || !this.definition || this.definition.pages.length <= 1) return;

    const hasFields = page.rows.some((row) => row.columns.length > 0);
    if (hasFields) {
      const target = this.pageIndex === 0 ? "next" : "previous";
      try {
        await umbConfirmModal(this, {
          headline: "Remove page",
          content: html`Remove <strong>${this.#pageName(this.pageIndex)}</strong>? Its
            fields move to the ${target} page.`,
          color: "danger",
          confirmLabel: "Remove",
        });
      } catch {
        // Cancelled
        return;
      }
    }
    this.context?.removePage(this.pageIndex);
  }

  #pageName(index: number) {
    return this.definition?.pages[index]?.title || `Page ${index + 1}`;
  }

  render() {
    const page = this.#page;
    if (!page || !this.definition) return nothing;

    const isFirst = this.pageIndex === 0;
    const isOnlyPage = this.definition.pages.length === 1;
    // A page can only depend on answers the visitor gave before reaching it
    const conditionFields = this.context?.getFieldsBeforePage(this.pageIndex, false) ?? [];

    return html`
      <div class="panel-header">
        <span class="panel-icon">
          <umb-icon name="icon-document"></umb-icon>
        </span>
        <div class="panel-title">
          <h3>${this.#pageName(this.pageIndex)}</h3>
          <span>Page ${this.pageIndex + 1} of ${this.definition.pages.length}</span>
        </div>
      </div>
      <div class="inspector-content">
        <uui-label for="page-title">Title</uui-label>
        <p class="description">Shown above the page and in the progress steps. Empty shows "Step ${this.pageIndex + 1}".</p>
        <uui-input
          id="page-title"
          label="Title"
          .value=${page.title ?? ""}
          @input=${(e: Event) => this.#onTextChange("title", e)}
        ></uui-input>

        <uui-label for="page-next">Next button</uui-label>
        <p class="description">Empty means Next. The last page shows the form's submit button instead.</p>
        <uui-input
          id="page-next"
          label="Next button"
          placeholder="Next"
          .value=${page.nextLabel ?? ""}
          @input=${(e: Event) => this.#onTextChange("nextLabel", e)}
        ></uui-input>

        <uui-label for="page-previous">Previous button</uui-label>
        <p class="description">Empty means Previous. The first page has no previous button.</p>
        <uui-input
          id="page-previous"
          label="Previous button"
          placeholder="Previous"
          .value=${page.previousLabel ?? ""}
          @input=${(e: Event) => this.#onTextChange("previousLabel", e)}
        ></uui-input>

        ${isFirst
          ? html`<p class="description">The first page is always shown.</p>`
          : html`
              <sf-condition-editor
                label="Show this page when"
                .condition=${page.visibility}
                .fields=${conditionFields}
                @condition-change=${(e: ConditionChangeEvent) => this.#update({ visibility: e.condition })}
              ></sf-condition-editor>
              <p class="description">
                A page that isn't shown is skipped, and its fields aren't validated. Its conditions can use the fields on earlier pages.
              </p>
            `}

        ${isOnlyPage
          ? nothing
          : html`
              <uui-button look="secondary" color="danger" label="Remove page" @click=${this.#removePage}>
                <uui-icon name="icon-trash"></uui-icon> Remove page
              </uui-button>
            `}
      </div>
    `;
  }

  static styles = css`
    .panel-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-4) var(--uui-size-space-5);
      border-bottom: 1px solid var(--uui-color-border);
    }

    .panel-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 36px;
      height: 36px;
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-surface-alt);
      color: var(--uui-color-interactive);
    }

    .panel-title {
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    .panel-title h3 {
      margin: 0;
      font-size: var(--uui-type-default-size);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .panel-title span {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .inspector-content {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
      padding: var(--uui-size-space-5);
    }

    uui-label {
      margin-top: var(--uui-size-space-3);
    }

    uui-input {
      width: 100%;
    }

    .description {
      margin: 0;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    sf-condition-editor {
      margin-top: var(--uui-size-space-5);
    }

    uui-button {
      align-self: flex-start;
      margin-top: var(--uui-size-space-5);
    }
  `;
}
