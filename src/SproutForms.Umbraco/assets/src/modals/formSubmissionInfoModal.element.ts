import {
  customElement,
  html,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { FormSubmissionInfoModalItem } from "../models";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { FormSubmissionBackofficeModel } from "../api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";

import "../workspaces/submissionEditors/submissionField.element";

@customElement("sf-submission-info-modal")
export default class FormSubmissionInfoModalElement extends UmbModalBaseElement<
  FormSubmissionInfoModalItem,
  FormSubmissionInfoModalItem
> {
  model?: UmbObjectState<FormSubmissionInfoModalItem>;

  @state()
  submission?: FormSubmissionBackofficeModel;

  override async connectedCallback() {
    super.connectedCallback();

    this.model = new UmbObjectState(this.data!);

    new SproutFormsSource(this)
      .getSubmission(this.data!.submissionId)
      .then((response) => {
        this.submission = response.data;
      });
  }

  render() {
    return html`
      <umb-body-layout headline="Submission Info">
        <uui-box>
          ${when(
            this.submission,
            () => html`
              <h2>Submission values</h2>
              ${repeat(
                this.submission!.values,
                (item) => item.name,
                (item) => html`<sf-submission-field
                  .fieldValue=${item}
                ></sf-submission-field>`,
              )}
            `
          )}
        </uui-box>

        <umb-workspace-footer slot="footer" data-mark="workspace:footer">
        </umb-workspace-footer>
      </umb-body-layout>
    `;
  }
}
