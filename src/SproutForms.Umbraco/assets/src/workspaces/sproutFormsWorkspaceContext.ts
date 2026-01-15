import {
  UmbContextBase,
  UmbControllerBase,
} from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import {
  UMB_WORKSPACE_CONTEXT,
  UmbRoutableWorkspaceContext,
  UmbWorkspaceContext,
  UmbWorkspaceRouteManager,
} from "@umbraco-cms/backoffice/workspace";
import { SproutFormsWorkspaceElement } from "./sproutFormsWorkspace.element";
import {
  FormBackofficeModel,
  FormColumnBackofficeModel,
  FormFieldBackofficeModel,
  FormRowBackofficeModel
} from "../api";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { SOURCE_UI } from "../models";

export default class SproutFormsWorkspaceContext
  extends UmbContextBase
  implements UmbWorkspaceContext, UmbRoutableWorkspaceContext
{
  workspaceAlias = "sproutForms.form.detail";

  routes = new UmbWorkspaceRouteManager(this);
  source = new SproutFormsSource(this);

  #updateAlias = true;

  #form = new UmbObjectState<FormBackofficeModel>({
    name: "",
    alias: "",
    version: 1,
    source: SOURCE_UI,
    definition: {
      rows: [],
      fields: [],
      workflows: [],
      outcome: {
        typeAlias: "message",
        displayName: "Show a message",
        configuration: {
          message: "Thank you for submitting",
        },
      },
    },
  });
  public readonly form = this.#form.asObservable();
  public readonly formId = this.#form.value.id;

  constructor(host: UmbControllerBase) {
    super(host, UMB_WORKSPACE_CONTEXT.toString());
    this.provideContext(SF_FORM_DETAIL_TOKEN_CONTEXT, this);

    this.routes.setRoutes([
      {
        path: "create",
        component: SproutFormsWorkspaceElement,
        setup: async () => {
          //this.load();
          console.log("create");
        },
      },
      {
        path: "edit/:unique",
        component: SproutFormsWorkspaceElement,
        setup: (_component, _info) => {
          this.#updateAlias = false;
          this.source.getForm(_info.match.params.unique).then((resp) => {
            this.#form.update(resp.data);
          });
        },
      },
    ]);
  }

  getFormId() {
    return this.#form.value.id;
  }

  setName(name: string) {
    this.#form.update({ name });
    if (!this.#updateAlias) return;

    this.source.generateAlias(name, this.#form.value.id!).then((result) => {
      this.#form.update({ alias: result.data });
    });
  }

  lockAliasUpdate() {
    this.#updateAlias = false;
  }

  updateForm(form: Partial<FormBackofficeModel>) {
    this.#form.update(form);
  }

  updateField(updatedField: Partial<FormFieldBackofficeModel>) {
    const clonedFields = structuredClone(this.#form.value.definition.fields);
    const field = clonedFields.find((f) => f.alias === updatedField.alias);
    if (field) {
      Object.assign(field, updatedField);
      this.#form.update({
        definition: {
          ...this.#form.value.definition,
          fields: [...clonedFields],
        },
      });
    }
  }

  setColumnSize(
    row: FormRowBackofficeModel,
    column: FormColumnBackofficeModel,
    newSize: number
  ) {
    const rowIndex = this.#form.value.definition.rows.findIndex(
      (r) => r === row
    );
    if (rowIndex === -1) return;

    const columnIndex = this.#form.value.definition.rows[
      rowIndex
    ].columns.findIndex((c) => c === column);
    if (columnIndex === -1) return;

    const updatedRows = structuredClone(this.#form.value.definition.rows);
    updatedRows[rowIndex].columns[columnIndex].width = newSize;

    this.#form.update({
      definition: {
        ...this.#form.value.definition,
        rows: updatedRows,
      },
    });
  }

  async save() {
    const returnValue = await this.source.saveForm(this.#form.value);
    this.#form.update(returnValue.data);

    history.replaceState(
      {},
      "",
      `/umbraco/section/sproutForms/workspace/sprout-form/edit/${returnValue.data.id}`
    );

    this.consumeContext(UMB_NOTIFICATION_CONTEXT, (notificationContext) => {
      notificationContext?.peek("positive", {
        data: {
          message: "Form saved successfully"
        },
      });
    });
  }

  getEntityType(): string {
    return "sf-form";
  }
}

export const SF_FORM_DETAIL_TOKEN_CONTEXT =
  new UmbContextToken<SproutFormsWorkspaceContext>(
    "sproutFormsWorkspaceContext"
  );
