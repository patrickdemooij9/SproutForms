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
import { UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import FormSubmissionsWorkspaceElement from "./formSubmissionsWorkspace.element";

export const FORM_SUBMISSIONS_ENTITY_TYPE = "sprout-form-submissions";

export function getFormSubmissionsPath(formId: string) {
  return `/umbraco/section/sproutForms/workspace/${FORM_SUBMISSIONS_ENTITY_TYPE}/edit/${formId}`;
}

/**
 * The submissions of one form, with its recycle bin: a page of its own, opened from the forms list.
 */
export default class FormSubmissionsWorkspaceContext
  extends UmbContextBase
  implements UmbWorkspaceContext, UmbRoutableWorkspaceContext
{
  workspaceAlias = "sproutForms.form.submissions";

  routes = new UmbWorkspaceRouteManager(this);

  #formId = new UmbStringState<string | undefined>(undefined);
  public readonly formId = this.#formId.asObservable();

  #formName = new UmbStringState<string | undefined>(undefined);
  public readonly formName = this.#formName.asObservable();

  constructor(host: UmbControllerBase) {
    super(host, UMB_WORKSPACE_CONTEXT.toString());
    this.provideContext(SF_FORM_SUBMISSIONS_TOKEN_CONTEXT, this);

    this.routes.setRoutes([
      {
        path: "edit/:unique",
        component: FormSubmissionsWorkspaceElement,
        setup: (_component, info) => {
          const formId = info.match.params.unique;
          this.#formId.setValue(formId);
          this.#formName.setValue(undefined);
          new SproutFormsSource(this).getForm(formId).then((resp) => {
            this.#formName.setValue(resp.data?.name);
          });
        },
      },
    ]);
  }

  getFormId() {
    return this.#formId.getValue();
  }

  getEntityType(): string {
    return FORM_SUBMISSIONS_ENTITY_TYPE;
  }
}

export const SF_FORM_SUBMISSIONS_TOKEN_CONTEXT =
  new UmbContextToken<FormSubmissionsWorkspaceContext>("sproutFormSubmissionsWorkspaceContext");
