import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbWorkspaceActionBase } from "@umbraco-cms/backoffice/workspace";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { SF_FORM_DETAIL_TOKEN_CONTEXT } from "../workspaces/sproutFormsWorkspaceContext";

// Every form has a recycle bin of its own, so each action works on the submissions of the open form
async function getFormId(host: UmbControllerBase) {
  const formContext = await host.getContext(SF_FORM_DETAIL_TOKEN_CONTEXT);
  return formContext?.getFormId();
}

async function reloadCollection(host: UmbControllerBase) {
  const collectionContext = await host.getContext(UMB_COLLECTION_CONTEXT);
  collectionContext?.loadCollection();
}

const describe = (count: number) => (count === 1 ? "1 submission" : `${count} submissions`);

// Moving to the recycle bin can be undone, so it doesn't ask for confirmation
async function trashSubmissions(host: UmbControllerBase, submissionIds: string[]) {
  const formId = await getFormId(host);
  if (!formId) return;

  const { error } = await new SproutFormsSource(host).trashSubmissions(formId, submissionIds);
  if (error) return;

  await reloadCollection(host);
}

async function restoreSubmissions(host: UmbControllerBase, submissionIds: string[]) {
  const formId = await getFormId(host);
  if (!formId) return;

  const { error } = await new SproutFormsSource(host).restoreSubmissions(formId, submissionIds);
  if (error) return;

  const notificationContext = await host.getContext(UMB_NOTIFICATION_CONTEXT);
  notificationContext?.peek("positive", { data: { message: `${describe(submissionIds.length)} restored` } });
  await reloadCollection(host);
}

// Deleting for good can't be undone, so it asks first
async function deleteSubmissionsPermanently(host: UmbControllerBase, submissionIds: string[]) {
  const formId = await getFormId(host);
  if (!formId) return;

  try {
    await umbConfirmModal(host, {
      headline: `Delete ${describe(submissionIds.length)} permanently`,
      content: "The submitted values and uploaded files are deleted. This can't be undone.",
      color: "danger",
      confirmLabel: "Delete permanently",
    });
  } catch {
    // Cancelled
    return;
  }

  const { error } = await new SproutFormsSource(host).deleteSubmissionsPermanently(formId, submissionIds);
  if (error) return;

  await reloadCollection(host);
}

export class TrashSubmissionsBulkAction extends UmbEntityBulkActionBase<object> {
  async execute() {
    await trashSubmissions(this, this.selection);
  }
}

export class TrashSubmissionEntityAction extends UmbEntityActionBase<never> {
  async execute() {
    if (!this.args.unique) return;
    await trashSubmissions(this, [this.args.unique]);
  }
}

export class RestoreSubmissionsBulkAction extends UmbEntityBulkActionBase<object> {
  async execute() {
    await restoreSubmissions(this, this.selection);
  }
}

export class RestoreSubmissionEntityAction extends UmbEntityActionBase<never> {
  async execute() {
    if (!this.args.unique) return;
    await restoreSubmissions(this, [this.args.unique]);
  }
}

export class DeleteSubmissionsPermanentlyBulkAction extends UmbEntityBulkActionBase<object> {
  async execute() {
    await deleteSubmissionsPermanently(this, this.selection);
  }
}

export class DeleteSubmissionPermanentlyEntityAction extends UmbEntityActionBase<never> {
  async execute() {
    if (!this.args.unique) return;
    await deleteSubmissionsPermanently(this, [this.args.unique]);
  }
}

export class EmptySubmissionsRecycleBinAction extends UmbWorkspaceActionBase {
  override async execute() {
    const formId = await getFormId(this);
    if (!formId) return;

    try {
      await umbConfirmModal(this, {
        headline: "Empty recycle bin",
        content: "Every submission in this form's recycle bin is deleted with its uploaded files. This can't be undone.",
        color: "danger",
        confirmLabel: "Empty recycle bin",
      });
    } catch {
      // Cancelled
      return;
    }

    const { error } = await new SproutFormsSource(this).emptySubmissionsRecycleBin(formId);
    if (error) return;

    await reloadCollection(this);
  }
}
