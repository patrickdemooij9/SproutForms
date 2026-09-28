import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  nothing,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UUIPaginationEvent } from "@umbraco-cms/backoffice/external/uui";
import { FormAuditAction, FormAuditEntryBackofficeModel } from "../api";
import { FormDto, SOURCE_CODE } from "../models";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { FORM_ROLLBACK_MODAL } from "../modals/formRollbackModal.token";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";

const PAGE_SIZE = 10;

const DATE_OPTIONS: Intl.DateTimeFormatOptions = {
  dateStyle: "long",
  timeStyle: "short",
};

// The same looks as the history of a document
const ACTION_TAGS: Record<FormAuditAction, { label: string; look: string; color: string }> = {
  [FormAuditAction.CREATED]: { label: "Created", look: "primary", color: "positive" },
  [FormAuditAction.SAVED]: { label: "Saved", look: "primary", color: "default" },
  [FormAuditAction.RENAMED]: { label: "Renamed", look: "secondary", color: "default" },
  [FormAuditAction.MOVED]: { label: "Moved", look: "secondary", color: "default" },
  [FormAuditAction.ROLLED_BACK]: { label: "Rollback", look: "secondary", color: "default" },
};

@customElement("form-info")
export class FormInfoElement extends UmbElementMixin(LitElement) {
  #source = new SproutFormsSource(this);
  #context?: SproutFormsWorkspaceContext;

  @state()
  private _form?: FormDto;

  @state()
  private _items?: Array<FormAuditEntryBackofficeModel>;

  @state()
  private _currentPage = 1;

  @state()
  private _totalPages = 1;

  constructor() {
    super();

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.#context = context;
      this.observe(context?.form, (form) => {
        const isOtherForm = form?.id !== this._form?.id;
        this._form = form;
        if (isOtherForm) this.#loadHistory();
      });
      // A save or rollback adds to the history
      this.observe(context?.storedCount, () => this.#loadHistory());
    });
  }

  async #loadHistory() {
    if (!this._form?.id) return;

    const { data } = await this.#source.getFormHistory(this._form.id, (this._currentPage - 1) * PAGE_SIZE, PAGE_SIZE);
    if (!data) return;

    this._items = data.items;
    this._totalPages = Math.max(1, Math.ceil(data.total / PAGE_SIZE));
  }

  #onPageChange(event: UUIPaginationEvent) {
    this._currentPage = event.target.current;
    this.#loadHistory();
  }

  async #openRollback() {
    if (!this._form?.id) return;

    try {
      const value = await umbOpenModal(this, FORM_ROLLBACK_MODAL, {
        data: { formId: this._form.id },
      });
      this.#context?.loadForm(value.form);
    } catch {
      // Cancelled
      return;
    }

    const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
    notificationContext?.peek("positive", {
      data: { message: "Form rolled back" },
    });
  }

  #renderHistory() {
    if (!this._items) return html`<div id="loader"><uui-loader></uui-loader></div>`;
    if (this._items.length === 0) {
      return html`<p>No history yet. Changes made from now on are shown here.</p>`;
    }

    return html`
      <umb-history-list>
        ${repeat(
          this._items,
          (item) => item.id,
          (item) => {
            const tag = ACTION_TAGS[item.action];
            return html`
              <umb-history-item
                .name=${item.userName}
                .detail=${this.localize.date(item.createdAt, DATE_OPTIONS)}
              >
                <umb-user-avatar slot="avatar" .name=${item.userName}></umb-user-avatar>
                <div class="log-type">
                  <uui-tag look=${tag.look} color=${tag.color}>${tag.label}</uui-tag>
                  <span>${this.#describe(item)}</span>
                </div>
              </umb-history-item>
            `;
          },
        )}
      </umb-history-list>
      ${when(
        this._totalPages > 1,
        () => html`
          <uui-pagination
            .current=${this._currentPage}
            .total=${this._totalPages}
            @change=${this.#onPageChange}
          ></uui-pagination>
        `,
      )}
    `;
  }

  #describe(item: FormAuditEntryBackofficeModel) {
    switch (item.action) {
      case FormAuditAction.CREATED:
        return "Form created";
      case FormAuditAction.SAVED:
        return item.version ? `Form saved as version ${item.version}` : "Form saved";
      default:
        return item.comment ?? nothing;
    }
  }

  override render() {
    if (!this._form) return nothing;

    const canRollback = this._form.source !== SOURCE_CODE;
    return html`
      <div id="container">
        <umb-workspace-info-app-layout headline="History">
          ${when(
            canRollback,
            () => html`
              <uui-button
                slot="header-actions"
                look="secondary"
                label="Rollback…"
                @click=${() => this.#openRollback()}
              >
                <umb-icon name="icon-history"></umb-icon> Rollback…
              </uui-button>
            `,
          )}
          <div id="content">${this.#renderHistory()}</div>
        </umb-workspace-info-app-layout>

        <uui-box headline="General">
          <div class="general-item">
            <strong>Version</strong>
            <span>${this._form.version}</span>
          </div>
          <div class="general-item">
            <strong>Alias</strong>
            <span>${this._form.alias}</span>
          </div>
          <div class="general-item">
            <strong>Source</strong>
            <span>${this._form.source === SOURCE_CODE ? "Code" : "Backoffice"}</span>
          </div>
          <div class="general-item">
            <strong>Id</strong>
            <span>${this._form.id}</span>
          </div>
        </uui-box>
      </div>
    `;
  }

  static override styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
        overflow: auto;
      }

      #container {
        display: grid;
        grid-template-columns: 1fr 350px;
        gap: var(--uui-size-layout-1);
        align-items: start;
        padding: var(--uui-size-layout-1);
      }

      #content {
        display: block;
        padding: var(--uui-size-space-4) var(--uui-size-space-5);
      }

      #loader {
        display: flex;
        justify-content: center;
      }

      .log-type {
        display: grid;
        grid-template-columns: var(--uui-size-40) auto;
        gap: var(--uui-size-layout-1);
      }

      .log-type uui-tag {
        justify-self: center;
        height: fit-content;
        margin-top: auto;
        margin-bottom: auto;
      }

      uui-pagination {
        display: flex;
        justify-content: center;
        margin-top: var(--uui-size-layout-1);
      }

      .general-item {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-1);
        word-break: break-all;
      }

      .general-item + .general-item {
        margin-top: var(--uui-size-space-4);
      }

      @media (max-width: 1100px) {
        #container {
          grid-template-columns: 1fr;
        }
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "form-info": FormInfoElement;
  }
}
