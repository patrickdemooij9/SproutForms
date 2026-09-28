import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { FormBackofficeModel } from "../api";

export const FORM_ROLLBACK_MODAL_ALIAS = "sproutForms.modal.form.rollback";

export type FormRollbackModalData = {
  formId: string;
};

export type FormRollbackModalValue = {
  // The form as it is after the rollback
  form: FormBackofficeModel;
};

// The same modal as Umbraco's own rollback of a document
export const FORM_ROLLBACK_MODAL = new UmbModalToken<
  FormRollbackModalData,
  FormRollbackModalValue
>(FORM_ROLLBACK_MODAL_ALIAS, {
  modal: {
    type: "sidebar",
    size: "full",
  },
});
