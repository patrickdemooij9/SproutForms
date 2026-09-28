import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";

// Moving to the recycle bin can be undone, so it doesn't ask for confirmation
async function trashForms(host: UmbControllerBase, formIds: string[]) {
  const { error } = await new SproutFormsSource(host).trashForms(formIds);
  // tryExecute has already shown the error, such as a code-first form in the selection
  if (error) return;

  const collectionContext = await host.getContext(UMB_COLLECTION_CONTEXT);
  collectionContext?.loadCollection();
}

export class TrashFormsBulkAction extends UmbEntityBulkActionBase<object> {
  async execute() {
    await trashForms(this, this.selection);
  }
}

export class TrashFormEntityAction extends UmbEntityActionBase<never> {
  async execute() {
    if (!this.args.unique) return;
    await trashForms(this, [this.args.unique]);
  }
}
