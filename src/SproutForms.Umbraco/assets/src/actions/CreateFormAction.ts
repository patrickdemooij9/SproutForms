import { UmbWorkspaceActionBase } from "@umbraco-cms/backoffice/workspace";
import SproutFormsListContext from "../workspaces/sproutFormsListContext";
import { SF_FOLDER_DETAIL_TOKEN_CONTEXT } from "../workspaces/folderWorkspaceContext";

export default class CreateFormAction extends UmbWorkspaceActionBase<SproutFormsListContext> {
  override async execute() {
    const folderContext = await this.getContext(SF_FOLDER_DETAIL_TOKEN_CONTEXT);

    let url = `/umbraco/section/sproutForms/workspace/sprout-form/create`;
    if (folderContext) {
      url += `/${folderContext.folderUnique}`;
    }

    history.pushState({}, "", url);
  }
}
