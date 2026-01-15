import { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { FormOverviewItem } from "../models";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";

export default class SproutFormsListContext extends UmbDefaultCollectionContext<
  FormOverviewItem,
  any
> {
  workspaceAlias = "sproutForms.collections.forms";

  constructor(host: UmbControllerBase) {
    super(host, UMB_WORKSPACE_CONTEXT.toString());

    this.provideContext(ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT, this);
  }
}

export const ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT =
  new UmbContextToken<SproutFormsListContext>("sproutFormsListContext");
