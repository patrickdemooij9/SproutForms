import { UmbWorkspaceActionBase } from "@umbraco-cms/backoffice/workspace";
import SproutFormsListContext from "../workspaces/sproutFormsListContext";

export default class CreateFormAction extends UmbWorkspaceActionBase<SproutFormsListContext> {
  override async execute() {
    let url = `/umbraco/section/sproutForms/workspace/sprout-form/create`;

    history.pushState(
      {},
      "",
      url
    );
  }
}