import {
  css,
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

  #getStatusDot(status: string) {
    if (status === "Succeeded") {
      return html`<span class="workflow-status-dot success"></span>`;
    } else if (status === "Failed") {
      return html`<span class="workflow-status-dot failed"></span>`;
    } else if (status === "Running") {
      return html`<span class="workflow-status-dot running"></span>`;
    } else {
      return html`<span class="workflow-status-dot default"></span>`;
    }
  }

  #getStatusText(status: string) {
    switch (status) {
      case "Succeeded":
        return "Succeeded";
      case "Failed":
        return "Failed";
      case "Running":
        return "Running";
      case "Retrying":
        return "Retrying";
      default:
        return "Pending";
    }
  }

  #renderWorkflowActionButtons(_stage: {
    workflowAlias: string;
    status: string;
  }) {
    /* if (stage.status === "Failed") {
      return html`
        <uui-button 
          look="secondary" 
          size="xs"
          @click=${() => this.#handleRetry(stage.workflowAlias)}
        >
          Retry
        </uui-button>
      `;
    }
    
    // TODO: Check if workflow supports manual approval based on workflow type configuration
    // For now, show Approve/Decline buttons for all non-completed workflows
    if (stage.status === "Pending" || stage.status === "Running") {
      return html`
        <uui-button 
          look="primary" 
          color="positive"
          size="xs"
          @click=${() => this.#handleApprove(stage.workflowAlias)}
        >
          Approve
        </uui-button>
        <uui-button 
          look="primary" 
          color="danger"
          size="xs"
          style="margin-left: 4px;"
          @click=${() => this.#handleDecline(stage.workflowAlias)}
        >
          Decline
        </uui-button>
      `;
    } */

    return html``;
  }

  /* async #handleRetry(workflowAlias: string) {
    if (!this.submission) return;
    
    const source = new SproutFormsSource(this);
    const result = await source.retryWorkflow(this.submission.id, workflowAlias);
    
    if (result.data) {
      // Refresh the submission data to show updated status
      const response = await source.getSubmission(this.data!.submissionId);
      this.submission = response.data;
    }
  }

  async #handleApprove(workflowAlias: string) {
    if (!this.submission) return;
    
    const source = new SproutFormsSource(this);
    const result = await source.approveWorkflow(this.submission.id, workflowAlias);
    
    if (result.data) {
      // Refresh the submission data to show updated status
      const response = await source.getSubmission(this.data!.submissionId);
      this.submission = response.data;
    }
  }

  async #handleDecline(workflowAlias: string) {
    if (!this.submission) return;
    
    const source = new SproutFormsSource(this);
    const result = await source.declineWorkflow(this.submission.id, workflowAlias);
    
    if (result.data) {
      // Refresh the submission data to show updated status
      const response = await source.getSubmission(this.data!.submissionId);
      this.submission = response.data;
    }
  } */

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
                (item) =>
                  html`<sf-submission-field
                    .fieldValue=${item}
                  ></sf-submission-field>`,
              )}
            `,
          )}
        </uui-box>

        <uui-box>
          ${when(
            this.submission?.workflowStages?.length,
            () => html`
              <h2>Workflow</h2>
              ${repeat(
                this.submission!.workflowStages,
                (stage) => stage.workflowAlias,
                (stage) => html`
                  <div class="workflow-item">
                    <div class="workflow-content">
                      ${this.#getStatusDot(stage.status)}
                      <span>${stage.displayName}</span>
                      <span class="workflow-status-text">
                        (${this.#getStatusText(stage.status)})</span
                      >
                    </div>
                    <div>${this.#renderWorkflowActionButtons(stage)}</div>
                  </div>
                `,
              )}
            `,
          )}
        </uui-box>

        <umb-workspace-footer slot="footer" data-mark="workspace:footer">
        </umb-workspace-footer>
      </umb-body-layout>
    `;
  }

  static styles = css`
    .workflow-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 0;
      border-bottom: 1px solid #eee;
    }

    .workflow-content {
      display: flex;
      align-items: center;
    }

    .workflow-status-text {
      color: #666;
      font-size: 12px;
      margin-left: 8px;
    }

    .workflow-status-dot {
      display: inline-block;
      width: 10px;
      height: 10px;
      border-radius: 50%;
      margin-right: 4px;

      &.success {
        background-color: #4CAF50;
      }

      &.failed {
        background-color: #F44336;
      }

      &.running {
        background-color: #2196F3;
        animation: pulse 1s infinite;
      }

      &.default {
        background-color: #9E9E9E;
      }
    }
  `;
}
