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
  mergeObservables,
  UmbArrayState,
  UmbNumberState,
  UmbObjectState,
} from "@umbraco-cms/backoffice/observable-api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import {
  FormColumnDto,
  FormDefinitionDto,
  FormDefinitionTypeDto,
  FormDto,
  FormFieldDto,
  FormPageDto,
  FormRowDto,
  SOURCE_UI,
} from "../models";
import { mapToDto, mapToPost } from "../mappings";

export default class SproutFormsWorkspaceContext
  extends UmbContextBase
  implements UmbWorkspaceContext, UmbRoutableWorkspaceContext
{
  workspaceAlias = "sproutForms.form.detail";

  routes = new UmbWorkspaceRouteManager(this);
  source = new SproutFormsSource(this);

  #updateAlias = true;

  #form = new UmbObjectState<FormDto>({
    name: "",
    alias: "",
    version: 1,
    source: SOURCE_UI,
    definition: {
      type: {
        typeAlias: "standard",
        displayName: "Standard form",
        settings: {},
      },
      pages: [{ id: crypto.randomUUID(), rows: [] }],
      fields: [],
      workflows: [],
      showProgress: true,
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

  // The page the Build tab shows and edits
  #currentPageIndex = new UmbNumberState(0);
  public readonly currentPageIndex = this.#currentPageIndex.asObservable();
  public readonly currentPage = mergeObservables(
    [this.form, this.#currentPageIndex.asObservable()],
    ([form, index]) => form.definition.pages[index],
  );

  #formTypes = new UmbArrayState<FormDefinitionTypeDto>([], (it) => it.alias);
  public readonly formTypes = this.#formTypes.asObservable();

  // Undefined until the form types are loaded, and for a code-first form whose type has no descriptor
  public readonly formType = mergeObservables(
    [this.form, this.formTypes],
    ([form, formTypes]) =>
      formTypes.find((it) => it.alias === form.definition.type.typeAlias),
  );

  #formTypesLoaded: Promise<void>;

  constructor(host: UmbControllerBase) {
    super(host, UMB_WORKSPACE_CONTEXT.toString());
    this.provideContext(SF_FORM_DETAIL_TOKEN_CONTEXT, this);

    this.#formTypesLoaded = this.source.getFormTypes().then((resp) => {
      this.#formTypes.setValue(resp.data ?? []);
    });

    this.routes.setRoutes([
      {
        path: "create/:formType/:parent",
        component: SproutFormsWorkspaceElement,
        setup: (_component, info) => {
          this.#startNewForm(info.match.params.formType, info.match.params.parent);
        },
      },
      {
        path: "create/:formType",
        component: SproutFormsWorkspaceElement,
        setup: (_component, info) => {
          this.#startNewForm(info.match.params.formType);
        },
      },
      {
        path: "edit/:unique",
        component: SproutFormsWorkspaceElement,
        setup: (_component, _info) => {
          this.#updateAlias = false;
          this.source.getForm(_info.match.params.unique).then((resp) => {
            this.#currentPageIndex.setValue(0);
            this.#form.update(mapToDto(resp.data));
          });
        },
      },
    ]);
  }

  // The type is chosen before the form is created, and can't be changed afterwards
  async #startNewForm(formTypeAlias: string, folderId?: string) {
    await this.#formTypesLoaded;
    const formType = this.#formTypes
      .getValue()
      .find((it) => it.alias === formTypeAlias);
    if (!formType) return;

    const settings: Record<string, unknown> = {};
    formType.properties.forEach((prop) => {
      settings[prop.alias] = prop.value ?? null;
    });
    this.#form.update({
      folderId: folderId,
      definition: {
        ...this.#form.value.definition,
        type: {
          typeAlias: formType.alias,
          displayName: formType.displayName,
          settings: settings,
        },
      },
    });
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

  updateForm(form: Partial<FormDto>) {
    this.#form.update(form);
  }

  updateField(updatedField: Partial<FormFieldDto>) {
    const clonedFields = structuredClone(this.#form.value.definition.fields);
    const field = clonedFields.find((f) => f.id === updatedField.id);
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

  getCurrentPageIndex(): number {
    return this.#currentPageIndex.getValue();
  }

  setCurrentPage(index: number) {
    const pageCount = this.#form.value.definition.pages.length;
    this.#currentPageIndex.setValue(Math.min(Math.max(index, 0), pageCount - 1));
  }

  #setPages(pages: FormPageDto[]) {
    this.#form.update({
      definition: {
        ...this.#form.value.definition,
        pages,
      },
    });
  }

  // Adds an empty page at the end and shows it
  addPage() {
    const pages = [...this.#form.value.definition.pages, { id: crypto.randomUUID(), rows: [] }];
    this.#setPages(pages);
    this.#currentPageIndex.setValue(pages.length - 1);
  }

  updatePage(index: number, changes: Partial<FormPageDto>) {
    this.#setPages(
      this.#form.value.definition.pages.map((page, i) => (i === index ? { ...page, ...changes } : page)),
    );
  }

  // The page's fields move to the page before it, or after it for the first page, so removing a page never removes fields
  removePage(index: number) {
    const pages = this.#form.value.definition.pages;
    if (pages.length <= 1) return;

    const targetIndex = index === 0 ? 1 : index - 1;
    const updatedPages = pages.map((page, i) =>
      i === targetIndex ? { ...page, rows: [...page.rows, ...pages[index].rows] } : page,
    );
    updatedPages.splice(index, 1);
    this.#setPages(updatedPages);
    this.#currentPageIndex.setValue(index === 0 ? 0 : index - 1);
  }

  movePage(fromIndex: number, toIndex: number) {
    const pages = [...this.#form.value.definition.pages];
    if (fromIndex === toIndex || toIndex < 0 || toIndex >= pages.length) return;

    const current = pages[this.#currentPageIndex.getValue()];
    const [moved] = pages.splice(fromIndex, 1);
    pages.splice(toIndex, 0, moved);
    this.#setPages(pages);
    this.#currentPageIndex.setValue(pages.indexOf(current));
  }

  getPageIndexOfField(fieldId: string): number {
    return this.#form.value.definition.pages.findIndex((page) =>
      page.rows.some((row) => row.columns.some((col) => col.fieldId === fieldId)),
    );
  }

  // Moves the field to a new row at the end of the page
  moveFieldToPage(fieldId: string, pageIndex: number) {
    const sourceIndex = this.getPageIndexOfField(fieldId);
    if (sourceIndex === -1 || sourceIndex === pageIndex) return;

    const pages = structuredClone(this.#form.value.definition.pages);
    const column = pages[sourceIndex].rows
      .flatMap((row) => row.columns)
      .find((col) => col.fieldId === fieldId)!;
    pages[sourceIndex].rows = pages[sourceIndex].rows
      .map((row) => ({ ...row, columns: row.columns.filter((col) => col !== column) }))
      .filter((row) => row.columns.length > 0);
    pages[pageIndex].rows.push({
      id: crypto.randomUUID(),
      columns: [{ ...column, width: 12 }],
    });
    this.#setPages(pages);
  }

  // The fields a condition may use: those on earlier pages, and on the page itself when includeOwnPage is set
  getFieldsBeforePage(pageIndex: number, includeOwnPage: boolean): FormFieldDto[] {
    const lastPage = includeOwnPage ? pageIndex : pageIndex - 1;
    const fieldIds = new Set(
      this.#form.value.definition.pages
        .slice(0, lastPage + 1)
        .flatMap((page) => page.rows)
        .flatMap((row) => row.columns)
        .map((col) => col.fieldId),
    );
    return this.#form.value.definition.fields.filter((field) => fieldIds.has(field.id));
  }

  getCurrentPageRows(): FormRowDto[] {
    return this.#form.value.definition.pages[this.#currentPageIndex.getValue()]?.rows ?? [];
  }

  // Returns a copy of the definition with the rows of the current page replaced
  withCurrentPageRows(definition: FormDefinitionDto, rows: FormRowDto[]): FormDefinitionDto {
    const index = this.#currentPageIndex.getValue();
    return {
      ...definition,
      pages: definition.pages.map((page, i) => (i === index ? { ...page, rows } : page)),
    };
  }

  setColumnSize(
    row: FormRowDto,
    column: FormColumnDto,
    newSize: number
  ) {
    const rows = this.getCurrentPageRows();
    const rowIndex = rows.findIndex((r) => r === row);
    if (rowIndex === -1) return;

    const columnIndex = rows[rowIndex].columns.findIndex((c) => c === column);
    if (columnIndex === -1) return;

    const updatedRows = structuredClone(rows);
    updatedRows[rowIndex].columns[columnIndex].width = newSize;

    this.#form.update({
      definition: this.withCurrentPageRows(this.#form.value.definition, updatedRows),
    });
  }

  moveField(
    fieldId: string,
    targetRow?: FormRowDto,
    targetColumn?: FormColumnDto
  ) {
    const rows = this.getCurrentPageRows();
    const sourceRowIndex = rows.findIndex(row => 
      row.columns.some(col => col.fieldId === fieldId)
    );
    if (sourceRowIndex === -1) return;

    const sourceRow = rows[sourceRowIndex];
    const sourceColumnIndex = sourceRow.columns.findIndex(col => col.fieldId === fieldId);
    if (sourceColumnIndex === -1) return;

    const updatedRows = structuredClone(rows);
    
    // Switch existing column with new field
    if (targetRow && targetColumn) {
      const targetRowIndex = updatedRows.findIndex(r => r.id === targetRow.id);
      if (targetRowIndex !== -1) {
        const targetColumnIndex = updatedRows[targetRowIndex].columns.findIndex(c => c.id === targetColumn.id);
        if (targetColumnIndex !== -1) {
          const sourceColumn = updatedRows[sourceRowIndex].columns[sourceColumnIndex];
          const targetColumnRef = updatedRows[targetRowIndex].columns[targetColumnIndex];

          const tempFieldId = sourceColumn.fieldId;
          sourceColumn.fieldId = targetColumnRef.fieldId;
          targetColumnRef.fieldId = tempFieldId;
        }
      }
    } else if (targetRow) { // Only row means that we are adding it to the left over space in the row
      const targetRowIndex = updatedRows.findIndex(r => r.id === targetRow.id);
      if (targetRowIndex !== -1) {
        const spaceLeft = 12 - updatedRows[targetRowIndex].columns.reduce((prev, cur) => prev + cur.width, 0);
        const newColumn: FormColumnDto = {
          id: crypto.randomUUID(),
          width: spaceLeft,
          fieldId: fieldId
        }
        updatedRows[targetRowIndex].columns.push(newColumn);
        updatedRows[sourceRowIndex].columns.splice(sourceColumnIndex, 1);

        if (updatedRows[sourceRowIndex].columns.length === 0) {
          updatedRows.splice(sourceRowIndex, 1);
        }
      }
    } else { // Completely new row
      const newRow: FormRowDto = {
        id: crypto.randomUUID(),
        columns: [
          {
            id: crypto.randomUUID(),
            fieldId: fieldId,
            width: 12,
          },
        ],
      };
      updatedRows.push(newRow);
      updatedRows[sourceRowIndex].columns.splice(sourceColumnIndex, 1);

      if (updatedRows[sourceRowIndex].columns.length === 0) {
        updatedRows.splice(sourceRowIndex, 1);
      }
    }

    this.#form.update({
      definition: this.withCurrentPageRows(this.#form.value.definition, updatedRows),
    });
  }

  removeField(fieldId: string) {
    const updatedPages = this.#form.value.definition.pages.map(page => ({
      ...page,
      rows: page.rows.map(row => ({
        ...row,
        columns: row.columns.filter(col => col.fieldId !== fieldId)
      })).filter(row => row.columns.length > 0),
    }));

    const updatedFields = this.#form.value.definition.fields.filter(f => f.id !== fieldId);

    this.#form.update({
      definition: {
        ...this.#form.value.definition,
        pages: updatedPages,
        fields: updatedFields,
      },
    });
  }

  removeWorkflow(workflowId: string) {
    const updatedWorkflows = this.#form.value.definition.workflows.filter(w => w.id !== workflowId);

    this.#form.update({
      definition: {
        ...this.#form.value.definition,
        workflows: updatedWorkflows,
      },
    });
  }

  reorderWorkflows(workflowIds: string[]) {
    const updatedWorkflows = workflowIds.map((id, index) => {
      const workflow = this.#form.value.definition.workflows.find(w => w.id === id);
      if (workflow) {
        return { ...workflow, order: index + 1 };
      }
      return null;
    }).filter((w): w is NonNullable<typeof w> => w !== null);

    this.#form.update({
      definition: {
        ...this.#form.value.definition,
        workflows: updatedWorkflows,
      },
    });
  }

  async save() {
    const returnValue = await this.source.saveForm(mapToPost(this.#form.value));
    // tryExecute has already shown the error, such as fields the form type doesn't allow
    if (!returnValue.data) return;

    this.#form.update(mapToDto(returnValue.data));

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
