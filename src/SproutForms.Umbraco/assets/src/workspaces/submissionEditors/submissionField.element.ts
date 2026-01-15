import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  property,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { html, LitElement } from "lit";
import {
  ISubmissionFieldElement,
  SubmissionFieldManifest,
} from "../../manifests/submissionFieldManifest";
import {
  createExtensionElement,
  UmbExtensionsManifestInitializer,
} from "@umbraco-cms/backoffice/extension-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { FormSubmissionValueBackofficeModel } from "../../api";

@customElement("sf-submission-field")
export default class SubmissionFieldElement extends UmbElementMixin(
  LitElement
) {
  @property({ type: Object })
  public set fieldValue(value: FormSubmissionValueBackofficeModel | undefined) {
    this._fieldValue = value;
    this.observePropertyView();
  }
  public get fieldValue() {
    return this._fieldValue;
  }
  private _fieldValue?: FormSubmissionValueBackofficeModel;

  @state()
  private _element?: ISubmissionFieldElement;

  private observePropertyView() {
    if (!this._fieldValue) {
      return;
    }

    new UmbExtensionsManifestInitializer(
      this,
      umbExtensionsRegistry,
      "submissionField",
      null,
      (documents) => {
        documents.forEach((document) => {
          const manifest =
            document.manifest as unknown as SubmissionFieldManifest;

          if (
            !manifest ||
            manifest.fieldTypeAlias !== this._fieldValue!.fieldTypeAlias
          ) {
            return;
          }
          this._gotPreviewer(manifest);
        });
      }
    );
  }

  private async _gotPreviewer(
    manifest?: SubmissionFieldManifest | null
  ): Promise<void> {
    if (!manifest) {
      return;
    }

    const el = await createExtensionElement(manifest);
    if (el) {
      this._element = el;
      this._element.value = this._fieldValue!;
    }
  }

  render() {
    return when(
      this._element,
      () => html`${this._element}`,
      () => html`
        <p><strong>${this._fieldValue?.name}:</strong> ${this._fieldValue?.value}</p>
      `
    );
  }
}
