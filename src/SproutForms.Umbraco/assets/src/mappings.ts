import { FormBackofficeModel } from "./api";
import { FormDto, FormFieldDto } from "./models";

export function mapToDto(model: FormBackofficeModel): FormDto {
  const fields = model.definition.fields.map<FormFieldDto>((field) => ({
    id: crypto.randomUUID(),
    ...field,
  }));

  return {
    id: model.id,
    name: model.name,
    alias: model.alias,
    version: model.version,
    source: model.source,
    definition: {
      rows: model.definition.rows.map((row) => ({
        id: crypto.randomUUID(),
        columns: row.columns.map((col) => ({
          width: col.width,
          fieldId: fields.find((f) => f.alias == col.fieldAlias)?.id!,
        })),
      })),
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
    name: model.name,
    alias: model.alias,
    version: model.version,
    source: model.source,
    definition: {
      rows: model.definition.rows.map((row) => ({
        columns: row.columns.map((col) => ({
          width: col.width,
          fieldAlias: model.definition.fields.find((f) => f.id == col.fieldId)
            ?.alias!,
        })),
      })),
      fields: model.definition.fields,
      outcome: {
        ...model.definition.outcome,
      },
      workflows: [...model.definition.workflows],
    },
  };
}
