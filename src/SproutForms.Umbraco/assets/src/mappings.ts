import { FormBackofficeModel, FormFieldBackofficeModel, FormRowBackofficeModel } from "./api";
import { FormDto, FormFieldDto, FormRowDto } from "./models";

// A column points at a field of its own container by alias; the editor uses ids, so aliases can change while editing
function mapRowsToDto(rows: FormRowBackofficeModel[], fields: FormFieldDto[]): FormRowDto[] {
  return rows.map((row) => ({
    id: crypto.randomUUID(),
    columns: row.columns.map((col) => ({
      id: crypto.randomUUID(),
      width: col.width,
      fieldId: fields.find((f) => f.alias == col.fieldAlias)?.id!,
    })),
  }));
}

function mapFieldsToDto(fields: FormFieldBackofficeModel[]): FormFieldDto[] {
  return fields.map((model) => {
    const { fields: children, rows, ...field } = model;
    const dto: FormFieldDto = { id: crypto.randomUUID(), ...field };
    // Only a field group has its own fields
    if (children) {
      dto.fields = mapFieldsToDto(children);
      dto.rows = mapRowsToDto(rows ?? [], dto.fields);
    }
    return dto;
  });
}

function mapRowsToPost(rows: FormRowDto[], fields: FormFieldDto[]): FormRowBackofficeModel[] {
  return rows.map((row) => ({
    columns: row.columns.map((col) => ({
      width: col.width,
      fieldAlias: fields.find((f) => f.id == col.fieldId)?.alias!,
    })),
  }));
}

function mapFieldsToPost(fields: FormFieldDto[]): FormFieldBackofficeModel[] {
  return fields.map(({ fields: children, rows, ...field }) => ({
    ...field,
    fields: children ? mapFieldsToPost(children) : null,
    rows: children ? mapRowsToPost(rows ?? [], children) : null,
  }));
}

export function mapToDto(model: FormBackofficeModel): FormDto {
  const fields = mapFieldsToDto(model.definition.fields);

  return {
    id: model.id,
    folderId: model.folderId,
    name: model.name,
    alias: model.alias,
    version: model.version,
    source: model.source,
    definition: {
      type: {
        ...model.definition.type,
      },
      pages: model.definition.pages.map((page) => ({
        ...page,
        id: crypto.randomUUID(),
        rows: mapRowsToDto(page.rows, fields),
      })),
      submitLabel: model.definition.submitLabel,
      showProgress: model.definition.showProgress,
      fields: fields,
      outcome: {
        ...model.definition.outcome,
      },
      workflows: model.definition.workflows.map((workflow) => ({
        id: crypto.randomUUID(),
        ...workflow
      })),
    },
  };
}

export function mapToPost(model: FormDto): FormBackofficeModel {
  return {
    id: model.id,
    folderId: model.folderId,
    name: model.name,
    alias: model.alias,
    version: model.version,
    source: model.source,
    definition: {
      type: {
        ...model.definition.type,
      },
      pages: model.definition.pages.map((page) => ({
        title: page.title,
        nextLabel: page.nextLabel,
        previousLabel: page.previousLabel,
        visibility: page.visibility,
        rows: mapRowsToPost(page.rows, model.definition.fields),
      })),
      submitLabel: model.definition.submitLabel,
      showProgress: model.definition.showProgress,
      fields: mapFieldsToPost(model.definition.fields),
      outcome: {
        ...model.definition.outcome,
      },
      workflows: [...model.definition.workflows],
    },
  };
}
