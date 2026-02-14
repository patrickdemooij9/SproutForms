import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
  UmbTreeAncestorsOfRequestArgs,
  UmbTreeChildrenOfRequestArgs,
  UmbTreeItemModel,
  UmbTreeRootItemsRequestArgs,
  UmbTreeRootModel,
  UmbTreeServerDataSourceBase,
} from "@umbraco-cms/backoffice/tree";
import { BackofficeSproutForms, FormTreeItemModel } from "../api";

export interface FormTreeItem extends UmbTreeItemModel {
	entityType: string;
	isDraft: boolean;
}

export interface FormTreeRoot extends UmbTreeRootModel {
}

export class FormTreeSource extends UmbTreeServerDataSourceBase<
  FormTreeItemModel,
  FormTreeItem
> {
  constructor(host: UmbControllerHost) {
    super(host, {
      getRootItems,
      getChildrenOf,
      getAncestorsOf,
      mapper,
    });
  }
}

const getRootItems = async (_args: UmbTreeRootItemsRequestArgs) => {
  const data = await BackofficeSproutForms.getUmbracoSproutFormsRoot();
  return data;
};

const getChildrenOf = async (args: UmbTreeChildrenOfRequestArgs) => {
  if (args.parent.unique === null) {
    return getRootItems(args);
  } else {
    // eslint-disable-next-line local-rules/no-direct-api-import
    const data =
      await BackofficeSproutForms.getUmbracoSproutFormsChildren({
        query: {
          parentUnique: args.parent.unique,
          skip: args.skip,
          take: args.take,
        },
      });
    return data;
  }
};

const getAncestorsOf = async (args: UmbTreeAncestorsOfRequestArgs) => {
  const response =
    await BackofficeSproutForms.getUmbracoSproutFormsAncestors({
      query: {
        descendantId: args.treeItem.unique,
      },
    });
  // Assuming response is an array of NamedEntityTreeItemResponseModel
  return response;
};

const mapper = (item: FormTreeItemModel): FormTreeItem => {
	const isFolder = item.itemType === 'Folder';
	return {
		unique: item.id,
		parent: {
			unique: null,
			entityType: 'sf-root',
		},
		name: item.name,
		entityType: isFolder ? 'sf-folder' : 'sf-form',
		hasChildren: item.hasChildren,
		isFolder: isFolder,
		icon: isFolder ? 'icon-folder' : 'icon-list',
		isDraft: false,
	};
};
