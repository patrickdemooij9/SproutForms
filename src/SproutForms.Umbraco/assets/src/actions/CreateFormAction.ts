import { UmbWorkspaceActionBase } from "@umbraco-cms/backoffice/workspace";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import SproutFormsListContext from "../workspaces/sproutFormsListContext";
import { SF_FOLDER_DETAIL_TOKEN_CONTEXT } from "../workspaces/folderWorkspaceContext";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { FORM_TYPE_PICKER_MODAL_ALIAS } from "../modals/formTypePickerModal.alias";
import { FormTypePickerModalData, FormTypePickerModalValue } from "../models";

export default class CreateFormAction extends UmbWorkspaceActionBase<SproutFormsListContext> {
  override async execute() {
    const formTypeAlias = await this.pickFormType();
    if (!formTypeAlias) return;

    const folderUnique = await this.getFolderUnique();

    let url = `/umbraco/section/sproutForms/workspace/sprout-form/create/${formTypeAlias}`;
    if (folderUnique) {
      url += `/${folderUnique}`;
    }

    history.pushState({}, "", url);
  }

  // A form's type is chosen once, when it is created; with a single type there is nothing to choose
  async pickFormType(): Promise<string | undefined> {
    const { data: formTypes } = await new SproutFormsSource(this).getFormTypes();
    if (!formTypes || formTypes.length === 0) return undefined;
    if (formTypes.length === 1) return formTypes[0].alias;

    const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
    const modal = modalManager!.open<FormTypePickerModalData, FormTypePickerModalValue>(
      this,
      FORM_TYPE_PICKER_MODAL_ALIAS,
      {
        modal: { type: "dialog" },
        data: { formTypes },
      },
    );

    try {
      const value = await modal.onSubmit();
      return value.formTypeAlias;
    } catch {
      // Cancelled
      return undefined;
    }
  }

  async getFolderUnique() {
    try {
      const folderContext = await this.getContext(
        SF_FOLDER_DETAIL_TOKEN_CONTEXT,
      );
      return folderContext?.folderUnique;
    } catch {
      return undefined;
    }
  }
}
