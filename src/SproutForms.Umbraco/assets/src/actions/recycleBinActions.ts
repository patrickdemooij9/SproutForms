import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbWorkspaceActionBase } from "@umbraco-cms/backoffice/workspace";
import { SproutFormsSource } from "../repositories/sproutFormsSource";

async function reloadCollection(host: UmbControllerBase) {
  const collectionContext = await host.getContext(UMB_COLLECTION_CONTEXT);
  collectionContext?.loadCollection();
}

async function notify(host: UmbControllerBase, message: string) {
  const notificationContext = await host.getContext(UMB_NOTIFICATION_CONTEXT);
  notificationContext?.peek("positive", { data: { message } });
}

async function restoreForms(host: UmbControllerBase, formIds: string[]) {
  const { error } = await new SproutFormsSource(host).restoreForms(formIds);
  if (error) return;

  await notify(host, formIds.length === 1 ? "Form restored" : `${formIds.length} forms restored`);
  await reloadCollection(host);
}

// Deleting for good can't be undone, so it asks first
async function deleteFormsPermanently(host: UmbControllerBase, formIds: string[]) {
  try {
    await umbConfirmModal(host, {
      headline: formIds.length === 1 ? "Delete form permanently" : `Delete ${formIds.length} forms permanently`,
      content:
        formIds.length === 1
          ? "The form is deleted with all its submissions, uploaded files and history. This can't be undone."
          : "The forms are deleted with all their submissions, uploaded files and history. This can't be undone.",
      color: "danger",
      confirmLabel: "Delete permanently",
    });
  } catch {
    // Cancelled
    return;
  }

  const { error } = await new SproutFormsSource(host).deleteFormsPermanently(formIds);
  if (error) return;

  await reloadCollection(host);
}

export class RestoreFormsBulkAction extends UmbEntityBulkActionBase<object> {
  async execute() {
    await restoreForms(this, this.selection);
  }
}

export class RestoreFormEntityAction extends UmbEntityActionBase<never> {
  async execute() {
    if (!this.args.unique) return;
    await restoreForms(this, [this.args.unique]);
  }
}

export class DeleteFormsPermanentlyBulkAction extends UmbEntityBulkActionBase<object> {
  async execute() {
    await deleteFormsPermanently(this, this.selection);
  }
}

export class DeleteFormPermanentlyEntityAction extends UmbEntityActionBase<never> {
  async execute() {
    if (!this.args.unique) return;
    await deleteFormsPermanently(this, [this.args.unique]);
  }
}

export class EmptyRecycleBinAction extends UmbWorkspaceActionBase {
  override async execute() {
    try {
      await umbConfirmModal(this, {
        headline: "Empty recycle bin",
        content: "Every form in the recycle bin is deleted with all its submissions, uploaded files and history. This can't be undone.",
        color: "danger",
        confirmLabel: "Empty recycle bin",
      });
    } catch {
      // Cancelled
      return;
    }

    const { error } = await new SproutFormsSource(this).emptyRecycleBin();
    if (error) return;

    await reloadCollection(this);
  }
}
