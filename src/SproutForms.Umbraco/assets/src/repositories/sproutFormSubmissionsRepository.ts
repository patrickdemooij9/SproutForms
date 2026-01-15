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
import { FormSubmissionOverviewItem } from "../models";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "../workspaces/sproutFormsWorkspaceContext";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

export default class FormSubmissionsRepository
  extends UmbRepositoryBase
  implements UmbCollectionRepository
{
  #source: SproutFormsSource = new SproutFormsSource(this);
  #context?: SproutFormsWorkspaceContext;

  constructor(host: UmbControllerHost) {
    super(host);
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.#context = context;
    });
  }

  async requestCollection(
    filter?: UmbCollectionFilterModel | undefined
  ): Promise<UmbRepositoryResponse<UmbPagedModel<any>>> {
    const data = await this.#source.getSubmissions(
      filter?.take ?? 10,
      filter?.skip ?? 0,
      this.#context?.getFormId() ?? ""
    );
    const result: UmbRepositoryResponse<
      UmbPagedModel<FormSubmissionOverviewItem>
    > = {
      data: {
        total: data.data!.total,
        items: data.data!.items.map((item) => ({
          unique: item.id.toString(),
          entityType: "sprout-submission",
          ...item,
        })),
      },
    };
    return result;
  }
}
