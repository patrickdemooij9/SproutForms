import {
  UmbTableColumn,
  UmbTableConfig,
  UmbTableDeselectedEvent,
  UmbTableElement,
  UmbTableItem,
  UmbTableSelectedEvent,
} from "@umbraco-cms/backoffice/components";
import { UMB_COLLECTION_CONTEXT, UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { TrashedFormItem } from "../models";

const DATE_OPTIONS: Intl.DateTimeFormatOptions = {
  dateStyle: "long",
  timeStyle: "short",
};

@customElement("sf-recycle-bin-collection")
export default class RecycleBinCollectionElement extends UmbLitElement {
  #context?: UmbDefaultCollectionContext<TrashedFormItem, any>;

  @state()
  private _tableConfig: UmbTableConfig = {
    allowSelection: true,
  };

  @state()
  private _tableColumns: Array<UmbTableColumn> = [
    { name: "Name", alias: "name" },
    { name: "Deleted", alias: "trashedAt" },
    { name: "Deleted by", alias: "trashedBy" },
    { name: "Restores to", alias: "folder" },
    { name: "Submissions", alias: "submissions" },
    { name: "", alias: "actions", elementName: "umb-entity-actions-table-column-view" },
  ];

  @state()
  private _tableItems: Array<UmbTableItem> = [];

  @state()
  private _selection: Array<string> = [];

  constructor() {
    super();

    this.consumeContext(UMB_COLLECTION_CONTEXT, (context) => {
      this.#context = context as UmbDefaultCollectionContext<TrashedFormItem, any>;
      this.observe(this.#context?.selection.selection, (selection) => {
        this._selection = (selection ?? []).filter((it) => it) as string[];
      });
      this.observe(this.#context?.items, (items) => {
        this._tableItems = (items ?? []).map((item) => this.#toTableItem(item));
      });
    });
  }

  #toTableItem(item: TrashedFormItem): UmbTableItem {
    return {
      id: item.unique,
      icon: "icon-trafic",
      data: [
        { columnAlias: "name", value: item.name },
        { columnAlias: "trashedAt", value: this.localize.date(item.trashedAt, DATE_OPTIONS) },
        { columnAlias: "trashedBy", value: item.trashedByName },
        { columnAlias: "folder", value: item.folderName ?? "The root" },
        { columnAlias: "submissions", value: item.totalSubmissions },
        {
          columnAlias: "actions",
          value: { entityType: item.entityType, unique: item.unique, name: item.name },
        },
      ],
    };
  }

  #onSelectionChange(event: UmbTableSelectedEvent | UmbTableDeselectedEvent) {
    event.stopPropagation();
    const table = event.target as UmbTableElement;
    this.#context?.selection.setSelection(table.selection);
  }

  // The collection shows its own message when the bin is empty
  render() {
    return html`
      <umb-table
        .config=${this._tableConfig}
        .columns=${this._tableColumns}
        .items=${this._tableItems}
        .selection=${this._selection}
        @selected=${this.#onSelectionChange}
        @deselected=${this.#onSelectionChange}
      ></umb-table>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-recycle-bin-collection": RecycleBinCollectionElement;
  }
}
