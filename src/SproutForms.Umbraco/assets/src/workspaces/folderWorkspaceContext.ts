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
import SproutFormsListElement from "./sproutFormsList.element";

export default class FolderWorkspaceContext
  extends UmbContextBase
  implements UmbWorkspaceContext, UmbRoutableWorkspaceContext
{
  workspaceAlias = "sproutForms.folder.detail";

  routes = new UmbWorkspaceRouteManager(this);

  public folderUnique: string = "";

  constructor(host: UmbControllerBase) {
    super(host, UMB_WORKSPACE_CONTEXT.toString());
    this.provideContext(SF_FOLDER_DETAIL_TOKEN_CONTEXT, this);

    this.routes.setRoutes([
      {
        path: "edit/:unique",
        component: SproutFormsListElement,
        setup: (_component, info) => {
          this.folderUnique = info.match.params.unique;
        },
      },
    ]);
  }

  getEntityType(): string {
    return "sf-folder";
  }
}

export const SF_FOLDER_DETAIL_TOKEN_CONTEXT =
  new UmbContextToken<FolderWorkspaceContext>("sproutFolderWorkspaceContext");
