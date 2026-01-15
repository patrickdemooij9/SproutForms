import {
  UmbTableColumn,
  UmbTableConfig,
  UmbTableDeselectedEvent,
  UmbTableElement,
  UmbTableItem,
  UmbTableSelectedEvent,
} from "@umbraco-cms/backoffice/components";
import {
  customElement,
  html,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

import "./formNameLayout.element";
import SproutFormSubmissionsListContext, {
  ST_SPROUT_FORMS_SUBMISSIONS_LIST_TOKEN_CONTEXT,
} from "../workspaces/sproutFormSubmissionsContext";

import "./formSubmissionNameLayout.element";

@customElement("sprout-forms-form-submission-collection")
export default class FormSubmissionCollectionElement extends UmbLitElement {
  #context?: SproutFormSubmissionsListContext;

  @state()
  private _tableConfig: UmbTableConfig = {
    allowSelection: true,
  };

  @state()
  private _tableColumns: Array<UmbTableColumn> = [
    {
      name: "Name",
      alias: "name",
      elementName: "sf-form-submission-name-column-layout",
    },
  ];

  @state()
  private _tableItems: Array<UmbTableItem> = [];

  @state()
  private _selection: Array<string> = [];

  constructor() {
    super();

    this.loadItems();
  }

  async loadItems() {
    this.consumeContext(
      ST_SPROUT_FORMS_SUBMISSIONS_LIST_TOKEN_CONTEXT,
      (instance) => {
        if (!instance) {
          return;
        }
        this.#context = instance;

        this.observe(
          this.#context.selection.selection,
          (selection) =>
            (this._selection = selection.filter((it) => it) as string[])
        );
        this.observe(this.#context.items, (items) => {
          this._tableItems = items.map<UmbTableItem>((item) => {
            return {
              id: item.unique,
              icon: "icon-trafic",
              data: [
                {
                  columnAlias: "name",
                  value: {
                    unique: item.unique,
                    name: item.name,
                  },
                },
              ],
            };
          });
        });
      }
    );
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
    `;
  }
}
