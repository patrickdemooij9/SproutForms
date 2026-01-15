import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  html,
  LitElement,
} from "@umbraco-cms/backoffice/external/lit";

@customElement("form-submissions")
export class FormSubmissionsElement extends UmbElementMixin(LitElement) {
  render() {
    return html`
      <umb-collection
        alias="sproutForms.collections.submissions"
      ></umb-collection>
    `;
  }
}
