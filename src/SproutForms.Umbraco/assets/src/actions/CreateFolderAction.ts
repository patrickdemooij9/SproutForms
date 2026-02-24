import { UmbWorkspaceActionBase } from "@umbraco-cms/backoffice/workspace";
import SproutFormsListContext, {
  ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT,
} from "../workspaces/sproutFormsListContext";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { SF_FOLDER_DETAIL_TOKEN_CONTEXT } from "../workspaces/folderWorkspaceContext";

export default class CreateFolderAction extends UmbWorkspaceActionBase<SproutFormsListContext> {
  override async execute() {
    const folderName = prompt("Enter folder name:");
    if (!folderName) return;

    const folderUnique = await this.getFolderUnique();

    const parentId = folderUnique
      ? folderUnique
      : undefined;

    const source = new SproutFormsSource(this);
    await source.saveFolder({
      name: folderName,
      parentId: parentId,
    });

    const context = await this.getContext(ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT);
    context?.loadCollection();
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
