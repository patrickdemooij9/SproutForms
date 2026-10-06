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
  CalculationRuleDto,
  FieldContainer,
  FormColumnDto,
  FormConditionalOutcomeDto,
  FormDefinitionDto,
  FormDefinitionTypeDto,
  FormDto,
  FormFieldDto,
  FormPageDto,
  FormRowDto,
  FormVariableDto,
  SOURCE_UI,
} from "../models";
import { mapToDto, mapToPost } from "../mappings";
import { ConditionDefinition, ConditionValueSource, FormBackofficeModel } from "../api";

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
      conditionalOutcomes: [],
      variables: [],
      calculations: [],
    },
  });
  public readonly form = this.#form.asObservable();
  public readonly variables = this.#form.asObservablePart((form) => form.definition.variables);
  public readonly formId = this.#form.value.id;

  // The page the Build tab shows and edits
  #currentPageIndex = new UmbNumberState(0);
  public readonly currentPageIndex = this.#currentPageIndex.asObservable();
  public readonly currentPage = mergeObservables(
    [this.form, this.#currentPageIndex.asObservable()],
    ([form, index]) => form.definition.pages[index],
  );

  // Counts the times the form was stored, so views can reload what a save or rollback changed, such as the history
  #storedCount = new UmbNumberState(0);
  public readonly storedCount = this.#storedCount.asObservable();

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

  // Replaces the form with a stored one, such as the form after a rollback
  loadForm(model: FormBackofficeModel) {
    this.#form.update(mapToDto(model));
    this.#storedCount.setValue(this.#storedCount.getValue() + 1);
  }

  lockAliasUpdate() {
    this.#updateAlias = false;
  }

  updateForm(form: Partial<FormDto>) {
    this.#form.update(form);
  }

  // Finds the field at any depth, so also the fields inside a field group
  getField(fieldId: string): FormFieldDto | undefined {
    return findField(this.#form.value.definition.fields, fieldId);
  }

  // The calculations the field owns follow it when its alias changes, and so do its own rules' conditions on its
  // answer, since a new rule starts from one. Other fields' conditions keep the old alias
  updateField(updatedField: Partial<FormFieldDto>) {
    const clonedFields = structuredClone(this.#form.value.definition.fields);
    const field = findField(clonedFields, updatedField.id!);
    if (!field) return;

    const oldAlias = field.alias;
    Object.assign(field, updatedField);
    let calculations = this.#form.value.definition.calculations;
    if (oldAlias !== field.alias) {
      field.rules = field.rules.map((rule) => ({ ...rule, condition: renameField(rule.condition, oldAlias, field.alias) }));
      calculations = calculations.map((rule) =>
        rule.ownerFieldAlias === oldAlias
          ? { ...rule, ownerFieldAlias: field.alias, condition: rule.condition && renameField(rule.condition, oldAlias, field.alias) }
          : rule,
      );
    }
    this.#updateDefinition({ fields: [...clonedFields], calculations });
  }

  #updateDefinition(changes: Partial<FormDefinitionDto>) {
    this.#form.update({
      definition: {
        ...this.#form.value.definition,
        ...changes,
      },
    });
  }

  updateVariables(variables: FormVariableDto[]) {
    this.#updateDefinition({ variables });
  }

  updateCalculations(calculations: CalculationRuleDto[]) {
    this.#updateDefinition({ calculations });
  }

  updateConditionalOutcomes(conditionalOutcomes: FormConditionalOutcomeDto[]) {
    this.#updateDefinition({ conditionalOutcomes });
  }

  getCurrentPageIndex(): number {
    return this.#currentPageIndex.getValue();
  }

  setCurrentPage(index: number) {
    const pageCount = this.#form.value.definition.pages.length;
    this.#currentPageIndex.setValue(Math.min(Math.max(index, 0), pageCount - 1));
  }

  #setPages(pages: FormPageDto[]) {
    this.#updateDefinition({ pages });
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

  // The fields a condition of the field may use. Inside a field group those are the other fields of its entry
  // and the fields the group's own conditions may use; a group's fields are never available outside it
  getConditionFields(fieldId: string): FormFieldDto[] {
    const container = this.getFieldContainer(fieldId);
    if (!container) return [];
    if (container.kind === "page") {
      return container.pageIndex === -1 ? [] : this.getFieldsBeforePage(container.pageIndex, true);
    }

    const pageIndex = this.getPageIndexOfField(container.groupId);
    const outerFields = pageIndex === -1
      ? []
      : this.getFieldsBeforePage(pageIndex, true).filter((field) => field.id !== container.groupId);
    return [...this.getContainerFields(container), ...outerFields];
  }

  getCurrentPageRows(): FormRowDto[] {
    return this.#form.value.definition.pages[this.#currentPageIndex.getValue()]?.rows ?? [];
  }

  // The page the field is placed on, or the field group it is in
  getFieldContainer(fieldId: string): FieldContainer | undefined {
    const definition = this.#form.value.definition;
    if (definition.fields.some((field) => field.id === fieldId)) {
      return { kind: "page", pageIndex: this.getPageIndexOfField(fieldId) };
    }
    const group = definition.fields.find((field) => field.fields?.some((child) => child.id === fieldId));
    return group ? { kind: "group", groupId: group.id } : undefined;
  }

  getContainerRows(container: FieldContainer): FormRowDto[] {
    return rowsOf(this.#form.value.definition, container) ?? [];
  }

  getContainerFields(container: FieldContainer): FormFieldDto[] {
    return fieldsOf(this.#form.value.definition, container) ?? [];
  }

  // Field groups only go one level deep, so a group can't be placed inside another
  canPlaceField(field: FormFieldDto | undefined, container: FieldContainer): boolean {
    return !!field && !(isFieldGroup(field) && container.kind === "group");
  }

  // Adds the field to the row, or to a new row at the end, and returns the row and column the canvas now renders
  addField(field: FormFieldDto, container: FieldContainer, rowId?: string) {
    if (!this.canPlaceField(field, container)) return undefined;

    // The workspace state is frozen, so change a copy
    const definition = structuredClone(this.#form.value.definition);
    const rows = rowsOf(definition, container);
    const fields = fieldsOf(definition, container);
    if (!rows || !fields) return undefined;

    let row = rows.find((it) => it.id === rowId);
    if (!row) {
      row = { id: crypto.randomUUID(), columns: [] };
      rows.push(row);
    }
    const column: FormColumnDto = {
      id: crypto.randomUUID(),
      width: 12 - row.columns.reduce((a, b) => a + b.width, 0),
      fieldId: field.id,
    };
    row.columns.push(column);
    fields.push(field);
    this.#form.update({ definition });

    const addedRow = this.getContainerRows(container).find((it) => it.id === row.id);
    return { row: addedRow, column: addedRow?.columns.find((it) => it.id === column.id) };
  }

  setColumnSize(
    container: FieldContainer,
    row: FormRowDto,
    column: FormColumnDto,
    newSize: number
  ) {
    const definition = structuredClone(this.#form.value.definition);
    const updatedColumn = rowsOf(definition, container)
      ?.find((r) => r.id === row.id)
      ?.columns.find((c) => c.id === column.id);
    if (!updatedColumn) return;

    updatedColumn.width = newSize;
    this.#form.update({ definition });
  }

  // Moves the field within its page or group, or into another one, such as from the page into a repeater
  moveField(
    fieldId: string,
    target: FieldContainer,
    targetRow?: FormRowDto,
    targetColumn?: FormColumnDto
  ) {
    const source = this.getFieldContainer(fieldId);
    if (!source) return;

    const definition = structuredClone(this.#form.value.definition);
    const sourceRows = rowsOf(definition, source);
    const targetRows = rowsOf(definition, target);
    const sourceFields = fieldsOf(definition, source);
    const targetFields = fieldsOf(definition, target);
    if (!sourceRows || !targetRows || !sourceFields || !targetFields) return;

    const sourceRow = sourceRows.find((row) => row.columns.some((col) => col.fieldId === fieldId));
    const sourceColumn = sourceRow?.columns.find((col) => col.fieldId === fieldId);
    if (!sourceRow || !sourceColumn) return;

    const isSameContainer = isSameFieldContainer(source, target);
    const field = findField(definition.fields, fieldId);

    // Switch existing column with new field
    if (targetRow && targetColumn) {
      const column = targetRows.find((r) => r.id === targetRow.id)?.columns.find((c) => c.id === targetColumn.id);
      if (!column || column.fieldId === fieldId) return;

      if (!isSameContainer) {
        // The other field takes the dragged field's place, so it changes container too
        const otherField = column.fieldId ? findField(definition.fields, column.fieldId) : undefined;
        if (!this.canPlaceField(field, target) || (otherField && !this.canPlaceField(otherField, source))) return;

        moveBetween(sourceFields, targetFields, fieldId);
        if (otherField) moveBetween(targetFields, sourceFields, otherField.id);
      }
      sourceColumn.fieldId = column.fieldId;
      column.fieldId = fieldId;
    } else {
      if (!this.canPlaceField(field, target)) return;

      const row = targetRow ? targetRows.find((r) => r.id === targetRow.id) : undefined;
      if (targetRow && !row) return;

      if (row) { // Only row means that we are adding it to the left over space in the row
        const spaceLeft = 12 - row.columns.reduce((prev, cur) => prev + cur.width, 0);
        row.columns.push({ id: crypto.randomUUID(), width: spaceLeft, fieldId });
      } else { // Completely new row
        targetRows.push({
          id: crypto.randomUUID(),
          columns: [{ id: crypto.randomUUID(), fieldId, width: 12 }],
        });
      }

      sourceRow.columns.splice(sourceRow.columns.indexOf(sourceColumn), 1);
      if (sourceRow.columns.length === 0) {
        sourceRows.splice(sourceRows.indexOf(sourceRow), 1);
      }
      if (!isSameContainer) {
        moveBetween(sourceFields, targetFields, fieldId);
      }
    }

    this.#form.update({ definition });
  }

  // Removing a field group removes the fields inside it too, and removing a field the calculations it owns
  removeField(fieldId: string) {
    const container = this.getFieldContainer(fieldId);
    if (!container) return;

    const definition = structuredClone(this.#form.value.definition);
    const removed = findField(definition.fields, fieldId)!;
    const removedAliases = [removed.alias, ...(removed.fields ?? []).map((child) => child.alias)];
    definition.calculations = definition.calculations.filter(
      (rule) => !rule.ownerFieldAlias || !removedAliases.includes(rule.ownerFieldAlias),
    );
    const withoutField = (rows: FormRowDto[]) => rows
      .map(row => ({ ...row, columns: row.columns.filter(col => col.fieldId !== fieldId) }))
      .filter(row => row.columns.length > 0);

    if (container.kind === "page") {
      definition.pages = definition.pages.map(page => ({ ...page, rows: withoutField(page.rows) }));
      definition.fields = definition.fields.filter(f => f.id !== fieldId);
    } else {
      const group = findField(definition.fields, container.groupId)!;
      group.rows = withoutField(group.rows ?? []);
      group.fields = (group.fields ?? []).filter(f => f.id !== fieldId);
    }

    this.#form.update({ definition });
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

    this.loadForm(returnValue.data);

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

// A field group, such as a repeater, holds fields of its own
export function isFieldGroup(field: FormFieldDto): boolean {
  return Array.isArray(field.fields);
}

export function isSameFieldContainer(a: FieldContainer | undefined, b: FieldContainer | undefined): boolean {
  if (!a || !b) return a === b;
  return a.kind === "page"
    ? b.kind === "page" && a.pageIndex === b.pageIndex
    : b.kind === "group" && a.groupId === b.groupId;
}

function renameField(condition: ConditionDefinition, oldAlias: string, newAlias: string): ConditionDefinition {
  return {
    ...condition,
    rules: condition.rules.map((rule) => ({
      ...rule,
      fieldAlias: !rule.variableAlias && rule.fieldAlias === oldAlias ? newAlias : rule.fieldAlias,
      value: rule.valueSource === ConditionValueSource.FIELD && rule.value === oldAlias ? newAlias : rule.value,
    })),
  };
}

// Groups only go one level deep, so a field is either top-level or inside a top-level group
function findField(fields: FormFieldDto[], fieldId: string): FormFieldDto | undefined {
  for (const field of fields) {
    if (field.id === fieldId) return field;
    const child = field.fields?.find((it) => it.id === fieldId);
    if (child) return child;
  }
  return undefined;
}

// The container's own arrays, so changing them changes the definition. A field group always has both
function rowsOf(definition: FormDefinitionDto, container: FieldContainer): FormRowDto[] | undefined {
  if (container.kind === "page") return definition.pages[container.pageIndex]?.rows;
  return definition.fields.find((field) => field.id === container.groupId)?.rows ?? undefined;
}

function fieldsOf(definition: FormDefinitionDto, container: FieldContainer): FormFieldDto[] | undefined {
  if (container.kind === "page") return definition.fields;
  return definition.fields.find((field) => field.id === container.groupId)?.fields ?? undefined;
}

function moveBetween(from: FormFieldDto[], to: FormFieldDto[], fieldId: string) {
  const index = from.findIndex((field) => field.id === fieldId);
  if (index !== -1) to.push(...from.splice(index, 1));
}
