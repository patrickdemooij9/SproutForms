import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { WorkflowTemplateBackofficeModel } from "../api";

export const WORKFLOW_TEMPLATE_MODAL_ALIAS = "sproutForms.modal.workflowTemplate";

export type WorkflowTemplateModalData = {
  // A template with an id is edited; without one, it is the starting point of a new template
  template?: Partial<WorkflowTemplateBackofficeModel>;
};

export type WorkflowTemplateModalValue = {
  template: WorkflowTemplateBackofficeModel;
};

export const WORKFLOW_TEMPLATE_MODAL = new UmbModalToken<
  WorkflowTemplateModalData,
  WorkflowTemplateModalValue
>(WORKFLOW_TEMPLATE_MODAL_ALIAS, {
  modal: {
    type: "sidebar",
    size: "medium",
  },
});
