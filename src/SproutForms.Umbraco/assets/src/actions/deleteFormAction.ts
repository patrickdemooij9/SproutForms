import {
  ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT,
} from "../workspaces/sproutFormsListContext";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";

export default class DeleteFormAction extends UmbEntityBulkActionBase<object> {
  async execute() {
    const repository = new SproutFormsSource(this._host);
    await repository.deleteForms(this.selection.map((item) => item));

    const context = await this.getContext(ST_SPROUT_FORMS_LIST_TOKEN_CONTEXT);
    context?.loadCollection();
  }
}
