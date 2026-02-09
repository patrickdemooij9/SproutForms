import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  property,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { css, html, LitElement } from "lit";
import { FormFlowTypeDto, FormWorkflowDto } from "../models";
import {
  UmbPropertyDatasetElement,
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";

export class FlowChangeEvent extends Event {
  static readonly TYPE = "flow-change";
  flow!: Partial<FormWorkflowDto>;

  constructor(detail?: Partial<FormWorkflowDto>) {
    super(FlowChangeEvent.TYPE, { bubbles: true, composed: true });
    if (detail) this.flow = detail;
  }
}

@customElement("sf-integration-type-inspector")
export default class FormIntegrationTypeInspectorElement extends UmbElementMixin(
  LitElement,
) {
  @property({ type: Object })
  public set flow(value: FormWorkflowDto) {
    this._flow = value;

    this._values = Object.entries(value.configuration).map(([key, value]) => ({
      alias: key,
      value: value,
    }));
    this._values.push({
      alias: "alias",
      value: value.alias,
    });
  }
  public get flow() {
    return this._flow;
  }
  private _flow!: FormWorkflowDto;

  @property({ type: Object })
  public flowType!: FormFlowTypeDto;

  @state()
  private _values: Array<UmbPropertyValueData> = [];

  @state()
  private _activeTab = "general";

  #onPropertyDataChange(e: Event) {
    const value = (e.target as UmbPropertyDatasetElement).value;

    const clonedWorkflow = structuredClone(this.flow);
    value.forEach((item) => {
      if (Object.keys(clonedWorkflow.configuration).includes(item.alias)) {
        clonedWorkflow.configuration[item.alias] = item.value as string;
      }
    });

    const flowChangeEvent = new FlowChangeEvent();
    flowChangeEvent.flow = clonedWorkflow;
    this.dispatchEvent(flowChangeEvent);
  }

  protected render() {
    return html`
      <div class="fields">
        <umb-property-dataset
          .value=${this._values}
          @change=${(e: Event) => this.#onPropertyDataChange(e)}
        >
          <uui-tab-group class="tab-group">
            <uui-tab
              label="General"
              @click=${() => (this._activeTab = "general")}
              active=""
            ></uui-tab>
            <uui-tab
              label="Advanced"
              @click=${() => (this._activeTab = "advanced")}
            ></uui-tab>
          </uui-tab-group>

          <div class="content">
            ${when(
              this._activeTab === "general",
              () =>
                html` ${repeat(
                  this.flowType?.configuration ?? [],
                  (prop) => prop.alias,
                  (prop) => html`
                    <umb-property
                      alias=${prop.alias}
                      label=${prop.displayName}
                      description=""
                      property-editor-ui-alias=${prop.propertyEditor}
                      .appearance=${{
                        labelOnTop: true,
                      }}
                      val
                    ></umb-property>
                  `,
                )}`,
            )}
            ${when(
              this._activeTab === "advanced",
              () => html`
                <umb-property
                  alias="alias"
                  label="Alias"
                  description="Alias of the field"
                  property-editor-ui-alias="Umb.PropertyEditorUi.TextBox"
                  val
                ></umb-property>
              `,
            )}
          </div>
        </umb-property-dataset>
      </div>
    `;
  }

  static styles = css`
    .content {
      padding: 18px 12px;
    }

    .tab-group {
      border-bottom: 1px solid #ccc;
    }
  `;
}
