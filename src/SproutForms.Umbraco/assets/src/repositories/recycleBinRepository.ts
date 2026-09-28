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
import { TRASHED_FORM_ENTITY_TYPE, TrashedFormItem } from "../models";

export default class RecycleBinRepository
  extends UmbRepositoryBase
  implements UmbCollectionRepository
{
  #source = new SproutFormsSource(this);

  async requestCollection(
    filter?: UmbCollectionFilterModel
  ): Promise<UmbRepositoryResponse<UmbPagedModel<TrashedFormItem>>> {
    const { data, error } = await this.#source.getRecycleBin(filter?.take ?? 50, filter?.skip ?? 0);
    if (!data) return { error };

    return {
      data: {
        total: data.total,
        items: data.items.map((item) => ({
          unique: item.id,
          entityType: TRASHED_FORM_ENTITY_TYPE,
          name: item.name,
          alias: item.alias,
          trashedAt: item.trashedAt,
          trashedByName: item.trashedByName,
          folderName: item.folderName,
          totalSubmissions: item.totalSubmissions,
        })),
      },
    };
  }
}
