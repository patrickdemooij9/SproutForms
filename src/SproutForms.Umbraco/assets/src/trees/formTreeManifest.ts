import { ManifestRepository } from "@umbraco-cms/backoffice/extension-registry";
import { ManifestTree, ManifestTreeItem } from "@umbraco-cms/backoffice/tree";
import { FormTreeRepository } from "./formTreeRepository";

export const treeRepository: ManifestRepository = {
  type: "repository",
  alias: "SproutFormTreeRepository",
  name: "SproutForm Tree repository",
  api: FormTreeRepository,
};

export const tree: ManifestTree = {
  type: "tree",
  kind: "default",
  alias: "SproutFormTree",
  name: "SproutForm tree",
  meta: {
    repositoryAlias: "SproutFormTreeRepository",
  },
};

export const treeItem: ManifestTreeItem = {
  type: "treeItem",
  alias: "SeoToolkitTreeItem",
  name: "SeoToolkit Tree Item",
  forEntityTypes: ["sf-form"],
};

export const rootTreeItem: ManifestTreeItem = {
  type: "treeItem",
  kind: "default",
  alias: "SeoToolkitTreeItem.Root",
  name: "SeoToolkit Root Tree Item",
  forEntityTypes: ["sf-root"],
};

export const TreeManifests = [treeRepository, tree, treeItem, rootTreeItem];
