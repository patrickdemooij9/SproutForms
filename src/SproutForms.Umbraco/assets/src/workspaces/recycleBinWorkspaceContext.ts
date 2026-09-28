import { UmbContextBase, UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import {
  UMB_WORKSPACE_CONTEXT,
  UmbRoutableWorkspaceContext,
  UmbWorkspaceContext,
  UmbWorkspaceRouteManager,
} from "@umbraco-cms/backoffice/workspace";
import RecycleBinWorkspaceElement from "./recycleBinWorkspace.element";

export const RECYCLE_BIN_ENTITY_TYPE = "sprout-forms-recycle-bin";
export const RECYCLE_BIN_PATH = `/umbraco/section/sproutForms/workspace/${RECYCLE_BIN_ENTITY_TYPE}/overview`;

export default class RecycleBinWorkspaceContext
  extends UmbContextBase
  implements UmbWorkspaceContext, UmbRoutableWorkspaceContext
{
  workspaceAlias = "sproutForms.recycleBin";

  routes = new UmbWorkspaceRouteManager(this);

  constructor(host: UmbControllerBase) {
    super(host, UMB_WORKSPACE_CONTEXT.toString());

    this.routes.setRoutes([
      {
        path: "overview",
        component: RecycleBinWorkspaceElement,
      },
    ]);
  }

  getEntityType(): string {
    return RECYCLE_BIN_ENTITY_TYPE;
  }
}
