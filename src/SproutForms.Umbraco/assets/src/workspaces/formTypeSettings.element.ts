import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  nothing,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import {
  UmbPropertyDatasetElement,
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { FormDefinitionTypeDto, FormDto, SOURCE_CODE } from "../models";

/**
 * The form's type and the type's settings. The type is chosen when the form is created, but its settings can be changed.
 */
@customElement("sf-form-type-settings")
export class FormTypeSettingsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private form?: FormDto;

  @state()
  private formTypes: Array<FormDefinitionTypeDto> = [];

  @state()
  private formType?: FormDefinitionTypeDto;

  @state()
  private _values: Array<UmbPropertyValueData> = [];

  constructor() {
    super();

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      this.observe(context?.formTypes, (formTypes) => {
        this.formTypes = formTypes ?? [];
      });
      this.observe(context?.formType, (formType) => {
        this.formType = formType;
      });
      this.observe(context?.form, (form) => {
        this.form = form;
        this._values = Object.entries(form?.definition.type.settings ?? {}).map(([alias, value]) => ({
          alias,
          value,
        }));
      });
    });
  }

  #onPropertyDataChange(e: Event) {
    const value = (e.target as UmbPropertyDatasetElement).value;

    const definition = structuredClone(this.form!.definition);
    value.forEach((item) => {
      if (Object.keys(definition.type.settings).includes(item.alias)) {
        definition.type.settings[item.alias] = item.value;
      }
    });
    this.context?.updateForm({ definition });
  }

  render() {
    if (!this.form) return nothing;

    // A standard form is all there is until another form type is registered
    const type = this.form.definition.type;
    if (this.formTypes.length < 2 && type.typeAlias === "standard") return nothing;

    // A code-first form's settings are set in code, so they're only shown
    return html`
      <uui-box>
        <div slot="headline" class="headline">
          ${type.displayName}
          <uui-tag look="secondary">Form type</uui-tag>
        </div>
        <p class="box-description">
          ${this.formType?.description ?? ""}
          The type is chosen when the form is created, and can't be changed.
        </p>
        <umb-property-dataset
          .value=${this._values}
          ?inert=${this.form.source === SOURCE_CODE}
          @change=${this.#onPropertyDataChange}
        >
          ${repeat(
            this.formType?.properties ?? [],
            (prop) => prop.alias,
            (prop) => html`
              <umb-property
                alias=${prop.alias}
                label=${prop.displayName}
                description=""
                property-editor-ui-alias=${prop.propertyEditor}
                .appearance=${{ labelOnTop: true }}
                val
              ></umb-property>
            `
          )}
        </umb-property-dataset>
      </uui-box>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    .headline {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }

    .box-description {
      margin: 0 0 var(--uui-size-space-5);
      color: var(--uui-color-text-alt);
    }
  `;
}
