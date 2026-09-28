import { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { TrashedFormItem } from "../models";

export const RECYCLE_BIN_COLLECTION_ALIAS = "sproutForms.collections.recycleBin";

export default class RecycleBinCollectionContext extends UmbDefaultCollectionContext<
  TrashedFormItem,
  any
> {
  constructor(host: UmbControllerBase) {
    super(host, `${RECYCLE_BIN_COLLECTION_ALIAS}.overview`);
  }
}
