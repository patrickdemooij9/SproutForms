import {
  UmbTableColumn,
  UmbTableConfig,
  UmbTableDeselectedEvent,
  UmbTableElement,
  UmbTableItem,
  UmbTableSelectedEvent,
} from "@umbraco-cms/backoffice/components";
import {
  css,
  customElement,
  html,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";

import SproutFormsListContext, {
  ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT,
} from "../workspaces/sproutFormsListContext";

import "./formNameLayout.element";
import "./formSubmissionsLayout.element";
import { SOURCE_UI } from "../models";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { RECYCLE_BIN_PATH } from "../workspaces/recycleBinWorkspaceContext";

@customElement("sprout-forms-forms-collection")
export default class FormCollectionElement extends UmbLitElement {
  #context?: SproutFormsListContext;

  @state()
  private _tableConfig: UmbTableConfig = {
    allowSelection: true,
  };

  @state()
  private _tableColumns: Array<UmbTableColumn> = [
    {
      name: "Name",
      alias: "name",
      elementName: 'sf-form-name-column-layout'
    },
    {
      name: "Source",
      alias: "source",
    },
    {
      name: "Submissions",
      alias: "submissions",
      elementName: "sf-form-submissions-column-layout",
    },
    {
      name: "",
      alias: "actions",
      elementName: "umb-entity-actions-table-column-view",
    },
  ];

  @state()
  private _tableItems: Array<UmbTableItem> = [];

  @state()
  private _selection: Array<string> = [];

  // The recycle bin is listed at the bottom of the root, like a folder
  @state()
  private _isRoot = false;

  @state()
  private _recycleBinCount = 0;

  constructor() {
    super();

    this.loadItems();
  }

  async loadItems() {
    this.consumeContext(ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT, (instance) => {
      if (!instance) {
        return;
      }
      this.#context = instance;

      this.observe(this.#context.filter, (filter) => {
        this._isRoot = !(filter as UmbCollectionFilterModel | undefined)?.filter;
      });

      this.observe(
        this.#context.selection.selection,
        (selection) =>
          (this._selection = selection.filter((it) => it) as string[])
      );
      this.observe(this.#context.items, (items) => {
        // Deleting a form changes the list and the recycle bin together
        this.#loadRecycleBinCount();
        this._tableItems = items.map<UmbTableItem>((item) => {
          return {
            id: item.unique,
            icon: item.entityType == "sf-form" ? "icon-trafic" : "icon-folder",
            data: [
              {
                columnAlias: "name",
                value: {
                    name: item.name,
                    unique: item.unique,
                    entityType: item.entityType
                },
              },
              {
                columnAlias: "source",
                value: item.source == SOURCE_UI ? "Backoffice" : "Code",
              },
              {
                columnAlias: "submissions",
                value:
                  item.entityType == "sf-form"
                    ? { unique: item.unique, total: item.totalSubmissions }
                    : undefined,
              },
              {
                columnAlias: "actions",
                // A code-first form can't be deleted in the backoffice
                value:
                  item.entityType == "sf-form" && item.source == SOURCE_UI
                    ? { entityType: "sprout-form", unique: item.unique, name: item.name }
                    : undefined,
              },
            ],
          };
        });
      });
    });
  }

  async #loadRecycleBinCount() {
    const { data } = await new SproutFormsSource(this).getRecycleBin(1, 0);
    this._recycleBinCount = data?.total ?? 0;
  }

  #onSelected(event: UmbTableSelectedEvent) {
    event.stopPropagation();
    const table = event.target as UmbTableElement;
    const selection = table.selection;
    this.#context?.selection.setSelection(selection);
  }

  #onDeselected(event: UmbTableDeselectedEvent) {
    event.stopPropagation();
    const table = event.target as UmbTableElement;
    const selection = table.selection;
    this.#context?.selection.setSelection(selection);
  }

  render() {
    return html`
      <umb-table
        .config=${this._tableConfig}
        .columns=${this._tableColumns}
        .items=${this._tableItems}
        .selection=${this._selection}
        @selected="${this.#onSelected}"
        @deselected="${this.#onDeselected}"
      >
      </umb-table>
      ${when(
        this._isRoot,
        () => html`
          <a id="recycle-bin" href=${RECYCLE_BIN_PATH}>
            <umb-icon name="icon-trash"></umb-icon>
            <span>Recycle bin</span>
            <span class="count">${this._recycleBinCount} ${this._recycleBinCount === 1 ? "form" : "forms"}</span>
          </a>
        `,
      )}
    `;
  }

  static styles = css`
    #recycle-bin {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      margin-top: var(--uui-size-space-4);
      padding: var(--uui-size-space-4) var(--uui-size-space-5);
      border-radius: var(--uui-border-radius);
      background-color: var(--uui-color-surface);
      box-shadow: var(--uui-shadow-depth-1);
      color: var(--uui-color-interactive);
      text-decoration: none;
    }

    #recycle-bin:hover {
      color: var(--uui-color-interactive-emphasis);
    }

    .count {
      margin-left: auto;
      color: var(--uui-color-text-alt);
    }
  `;
}
