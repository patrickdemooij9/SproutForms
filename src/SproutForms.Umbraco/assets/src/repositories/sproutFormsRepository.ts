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
import { FormOverviewItem } from "../models";

export default class FormsRepository
  extends UmbRepositoryBase
  implements UmbCollectionRepository
{
  #source: SproutFormsSource = new SproutFormsSource(this);

  async requestCollection(
    filter?: UmbCollectionFilterModel | undefined
  ): Promise<UmbRepositoryResponse<UmbPagedModel<any>>> {
    const data = await this.#source.getForms(
      filter?.take ?? 10,
      filter?.skip ?? 0
    );
    const result: UmbRepositoryResponse<UmbPagedModel<FormOverviewItem>> = {
      data: {
        total: data.data!.total,
        items: data.data!.items.map((item) => ({
          unique: item.id.toString(),
          entityType: "sprout-form",
          ...item,
        })),
      },
    };
    return result;
  }
}
