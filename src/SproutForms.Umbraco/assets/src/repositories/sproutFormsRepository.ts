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
      filter?.skip ?? 0,
      filter?.filter
    );
    const result: UmbRepositoryResponse<UmbPagedModel<FormOverviewItem>> = {
      data: {
        total: data.data!.total,
        items: data.data!.items.map((item) => ({
          id: item.id,
          unique: item.id.toString(),
          entityType: item.itemType === 'Folder' ? 'sf-folder' : 'sf-form',
          name: item.name,
          hasChildren: item.hasChildren,
          source: item.source ?? 0,
          totalSubmissions: item.totalSubmissions ?? 0
        })),
      },
    };
    return result;
  }
}
