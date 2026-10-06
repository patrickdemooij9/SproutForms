import {
  UmbCollectionFilterModel,
  UmbCollectionRepository,
} from "@umbraco-cms/backoffice/collection";
import {
  UmbPagedModel,
  UmbRepositoryBase,
  UmbRepositoryResponse,
} from "@umbraco-cms/backoffice/repository";
import { SproutFormsSource } from "./sproutFormsSource";
import { FormSubmissionOverviewItem, SUBMISSION_ENTITY_TYPE, TRASHED_SUBMISSION_ENTITY_TYPE } from "../models";
import FormSubmissionsWorkspaceContext, {
  SF_FORM_SUBMISSIONS_TOKEN_CONTEXT,
} from "../workspaces/formSubmissionsWorkspaceContext";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

export default class FormSubmissionsRepository
  extends UmbRepositoryBase
  implements UmbCollectionRepository
{
  #source: SproutFormsSource = new SproutFormsSource(this);
  #context?: FormSubmissionsWorkspaceContext;

  // The repository of the form's recycle bin lists the trashed submissions instead
  protected trashed = false;

  constructor(host: UmbControllerHost) {
    super(host);
    this.consumeContext(SF_FORM_SUBMISSIONS_TOKEN_CONTEXT, (context) => {
      this.#context = context;
    });
  }

  async requestCollection(
    filter?: UmbCollectionFilterModel | undefined
  ): Promise<UmbRepositoryResponse<UmbPagedModel<any>>> {
    const data = await this.#source.getSubmissions(
      filter?.take ?? 10,
      filter?.skip ?? 0,
      this.#context?.getFormId() ?? "",
      this.trashed
    );
    const result: UmbRepositoryResponse<
      UmbPagedModel<FormSubmissionOverviewItem>
    > = {
      data: {
        total: data.data!.total,
        items: data.data!.items.map((item) => ({
          unique: item.id.toString(),
          ...item,
          entityType: this.trashed ? TRASHED_SUBMISSION_ENTITY_TYPE : SUBMISSION_ENTITY_TYPE,
        })),
      },
    };
    return result;
  }
}

export class TrashedFormSubmissionsRepository extends FormSubmissionsRepository {
  protected override trashed = true;
}
