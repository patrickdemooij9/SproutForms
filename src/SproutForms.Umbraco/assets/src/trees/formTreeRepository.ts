import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryResponse } from "@umbraco-cms/backoffice/repository";
import {
  UmbTreeItemModel,
  UmbTreeRepositoryBase,
  UmbTreeRootModel,
} from "@umbraco-cms/backoffice/tree";
import { FormTreeSource } from "./formTreeSource";

export class FormTreeRepository extends UmbTreeRepositoryBase<
  UmbTreeItemModel,
  UmbTreeRootModel
> {
  constructor(host: UmbControllerHost) {
    super(host, FormTreeSource);
  }

  async requestTreeRoot(): Promise<UmbRepositoryResponse<UmbTreeRootModel>> {
    
    const data: UmbTreeRootModel = {
      unique: null,
      entityType: "sf-root",
      name: "Forms",
      hasChildren: true,
      isFolder: true,
    };

    return { data };
  }
}
