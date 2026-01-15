import { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { FormSubmissionOverviewItem } from "../models";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";

export default class SproutFormSubmissionsListContext extends UmbDefaultCollectionContext<
  FormSubmissionOverviewItem,
  any
> {
  workspaceAlias = "sproutForms.collections.submissions";

  constructor(host: UmbControllerBase) {
    super(host, UMB_WORKSPACE_CONTEXT.toString());

    this.provideContext(ST_SPROUT_FORMS_SUBMISSIONS_LIST_TOKEN_CONTEXT, this);
  }
}

export const ST_SPROUT_FORMS_SUBMISSIONS_LIST_TOKEN_CONTEXT =
  new UmbContextToken<SproutFormSubmissionsListContext>("sproutFormSubmissionsListContext");
