import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  html,
  LitElement,
  css,
  property,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import "./sproutFormsList.element";
import "./templates.element";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { DashboardViewModel } from "../api";

@customElement("sprout-forms-dashboard")
export class SproutFormsDashboardElement extends UmbElementMixin(LitElement) {
  @property({ type: Object })
  data?: DashboardViewModel;

  @state()
  private activeTab: "dashboard" | "forms" | "templates" = "dashboard";

  constructor() {
    super();

    new SproutFormsSource(this).getDashboardInfo().then((resp) => {
      this.data = resp.data;
    });
  }

  render() {
    return html`
      <div class="tabs">
        <button
          class="tab ${this.activeTab === "dashboard" ? "active" : ""}"
          @click=${() => (this.activeTab = "dashboard")}
        >
          Dashboard
        </button>
        <button
          class="tab ${this.activeTab === "forms" ? "active" : ""}"
          @click=${() => (this.activeTab = "forms")}
        >
          Forms
        </button>
        <button
          class="tab ${this.activeTab === "templates" ? "active" : ""}"
          @click=${() => (this.activeTab = "templates")}
        >
          Templates
        </button>
      </div>

      <div
        class="tab-content ${this.activeTab === "dashboard" ? "active" : ""}"
      >
        <div class="dashboard">${this.renderDashboard()}</div>
      </div>

      <div class="tab-content ${this.activeTab === "forms" ? "active" : ""}">
        <sprout-forms-list></sprout-forms-list>
      </div>

      <div class="tab-content ${this.activeTab === "templates" ? "active" : ""}">
        <sf-templates></sf-templates>
      </div>
    `;
  }

  private renderDashboard() {
    if (!this.data) {
      return;
    }

    const {
      heroMetrics,
      submissionTrend,
      formActivity,
      workflowFeed,
      recentSubmissions,
    } = this.data;

    const maxCount = Math.max(
      ...submissionTrend.data.map((d) => d.submissionCount),
      1,
    );
    const yAxisLabels = this.generateYAxisLabels(maxCount);

    return html`
      <div class="dashboard-header">
        <div
          style="display: flex; justify-content: space-between; align-items: center;"
        >
          <div>
            <h1>Command Center</h1>
            <p>Monitor your forms and workflow health in real-time</p>
          </div>
        </div>
      </div>

      <div class="metrics-grid">
        <div class="metric-card">
          <div class="metric-label">Total Submissions (30d)</div>
          <div class="metric-value">
            ${heroMetrics.totalSubmissionsLast30Days}
          </div>
          <div
            class="metric-change ${this.getChangeClass(
              heroMetrics.submissionsChangePercent,
            )}"
          >
            ${this.renderChange(heroMetrics.submissionsChangePercent)} vs
            previous 30 days
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-label">Submissions Today</div>
          <div class="metric-value">${heroMetrics.submissionsToday}</div>
        </div>

        <div class="metric-card">
          <div class="metric-label">Workflow Success Rate (30d)</div>
          <div
            class="success-rate-badge ${this.getSuccessRateClass(
              heroMetrics.workflowSuccessRate,
            )}"
          >
            ${heroMetrics.workflowSuccessRate}%
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-label">Failed Workflows (7d)</div>
          <div
            class="metric-value"
            style="color: ${heroMetrics.failedWorkflowsLast7Days > 0
              ? "#ef4444"
              : "inherit"}"
          >
            ${heroMetrics.failedWorkflowsLast7Days}
          </div>
        </div>
      </div>

      <div class="chart-section">
        <h3 class="section-title">Submission Trend (Last 30 Days)</h3>
        <div class="chart-wrapper">
          <div class="chart-y-axis">
            ${yAxisLabels.map((label) => html`<span>${label}</span>`)}
          </div>
          <div class="chart-container">
            ${submissionTrend.data.map(
              (d) => html`
                <div
                  class="chart-bar"
                  style="height: ${(d.submissionCount / maxCount) * 100}%"
                  title="${d.date}: ${d.submissionCount} submissions"
                  data-count="${d.submissionCount}"
                ></div>
              `,
            )}
          </div>
        </div>
        <div class="chart-x-axis">
          <span>30 days ago</span>
          <span>15 days ago</span>
          <span>Today</span>
        </div>
      </div>

      <div class="table-section">
        <h3 class="section-title">Form Activity</h3>
        <table class="data-table">
          <thead>
            <tr>
              <th>Form Name</th>
              <th>Submissions (30d)</th>
              <th>Last Submission</th>
              <th>Workflow Failures</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            ${formActivity.forms.length === 0
              ? html`
                  <tr>
                    <td
                      colspan="5"
                      style="text-align: center; opacity: 0.6; padding: 40px;"
                    >
                      No forms found
                    </td>
                  </tr>
                `
              : formActivity.forms.map(
                  (form) => html`
                    <tr>
                      <td><strong>${form.formName}</strong></td>
                      <td>${form.submissionsLast30Days}</td>
                      <td>${this.formatDate(form.lastSubmissionDate!)}</td>
                      <td
                        style="color: ${form.workflowFailuresLast30Days > 0
                          ? "#ef4444"
                          : "inherit"}"
                      >
                        ${form.workflowFailuresLast30Days}
                      </td>
                      <td>
                        <span class="status-badge ${form.status.toLowerCase()}">
                          ${form.status}
                        </span>
                      </td>
                    </tr>
                  `,
                )}
          </tbody>
        </table>
      </div>

      <div class="bottom-grid">
        <div class="feed-section">
          <h3 class="section-title">Workflow Activity Feed</h3>
          ${workflowFeed.items.length === 0
            ? html`<div class="empty-state">No recent workflow activity</div>`
            : workflowFeed.items.map(
                (item) => html`
                  <div class="feed-item">
                    <div
                      class="feed-icon ${item.success ? "success" : "failure"}"
                    >
                      ${item.success
                        ? html`<svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="16"
                            height="16"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                          >
                            <polyline points="20 6 9 17 4 12"></polyline>
                          </svg>`
                        : html`<svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="16"
                            height="16"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                          >
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                          </svg>`}
                    </div>
                    <div class="feed-content">
                      <div class="feed-title">
                        ${item.workflowType} - ${item.workflowAlias}
                      </div>
                      <div class="feed-meta">
                        ${item.formName} -
                        ${this.formatRelativeTime(item.timestamp)}
                      </div>
                    </div>
                  </div>
                `,
              )}
        </div>

        <div class="submissions-section">
          <h3 class="section-title">Recent Submissions</h3>
          ${!recentSubmissions || recentSubmissions.items.length === 0
            ? html`<div class="empty-state">No recent submissions</div>`
            : recentSubmissions.items.slice(0, 5).map(
                (submission) => html`
                  <div class="submission-item">
                    <div class="submission-icon">
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="16"
                        height="16"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="2"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      >
                        <path
                          d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"
                        ></path>
                        <polyline points="14 2 14 8 20 8"></polyline>
                        <line x1="16" y1="13" x2="8" y2="13"></line>
                        <line x1="16" y1="17" x2="8" y2="17"></line>
                        <polyline points="10 9 9 9 8 9"></polyline>
                      </svg>
                    </div>
                    <div class="submission-content">
                      <div class="submission-title">${submission.formName}</div>
                      <div class="submission-meta">
                        ${this.formatRelativeTime(submission.timestamp)}
                      </div>
                    </div>
                  </div>
                `,
              )}
        </div>
      </div>
    `;
  }

  private generateYAxisLabels(maxCount: number): string[] {
    const steps = 5;
    const labels: string[] = [];
    for (let i = steps; i >= 0; i--) {
      const value = Math.round((maxCount / steps) * i);
      labels.push(value.toString());
    }
    return labels;
  }

  private getChangeClass(value: number): string {
    if (value > 0) return "positive";
    if (value < 0) return "negative";
    return "neutral";
  }

  private renderChange(value: number): string {
    if (value > 0) return `+${value}%`;
    if (value < 0) return `${value}%`;
    return "-";
  }

  private getSuccessRateClass(rate: number): string {
    if (rate > 95) return "high";
    if (rate >= 80) return "medium";
    return "low";
  }

  private formatDate(dateStr: string | null): string {
    if (!dateStr) return "Never";
    const date = new Date(dateStr);
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  private formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    return `${diffDays}d ago`;
  }

  static styles = css`
    :host {
      display: block;
      background: var(--uui-color-surface);
    }

    .tabs {
      display: flex;
      gap: 0;
      border-bottom: 1px solid var(--uui-color-border);
      padding: 0 20px;
    }

    .tab {
      padding: 16px 24px;
      font-size: 14px;
      font-weight: 500;
      color: var(--uui-color-text);
      background: transparent;
      border: none;
      border-bottom: 2px solid transparent;
      cursor: pointer;
      transition: all 0.2s;
      margin-bottom: -1px;
    }

    .tab:hover {
      color: var(--uui-color-headline);
    }

    .tab.active {
      color: var(--uui-color-selected);
      border-bottom-color: var(--uui-color-selected);
    }

    .tab-content {
      display: none;
    }

    .tab-content.active {
      display: block;
    }

    .dashboard {
      padding: 24px;

      background: var(--umb-body-layout-color-background, var(--uui-color-background));
    }

    .dashboard-header {
      margin-bottom: 24px;
    }

    .dashboard-header h1 {
      font-size: 24px;
      font-weight: 600;
      color: var(--uui-color-headline);
      margin: 0;
    }

    .dashboard-header p {
      font-size: 14px;
      color: var(--uui-color-text);
      opacity: 0.7;
      margin: 4px 0 0 0;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      margin-bottom: 24px;
    }

    @media (max-width: 1200px) {
      .metrics-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    .metric-card {
      background: white;
      border-radius: 8px;
      padding: 20px;
      border: 1px solid var(--uui-color-border);
    }

    .metric-label {
      font-size: 12px;
      font-weight: 500;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: var(--uui-color-text);
      opacity: 0.6;
      margin-bottom: 8px;
    }

    .metric-value {
      font-size: 32px;
      font-weight: 700;
      color: var(--uui-color-headline);
      line-height: 1;
    }

    .metric-change {
      font-size: 13px;
      font-weight: 500;
      margin-top: 8px;
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .metric-change.positive {
      color: #10b981;
    }

    .metric-change.negative {
      color: #ef4444;
    }

    .metric-change.neutral {
      color: var(--uui-color-text);
      opacity: 0.6;
    }

    .chart-section {
      background: white;
      border-radius: 8px;
      padding: 20px;
      margin-bottom: 24px;
      border: 1px solid var(--uui-color-border);
    }

    .section-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--uui-color-headline);
      margin: 0 0 16px 0;
    }

    .chart-wrapper {
      position: relative;
      height: 220px;
      padding-left: 40px;
      padding-bottom: 30px;
    }

    .chart-y-axis {
      position: absolute;
      left: 0;
      top: 0;
      bottom: 30px;
      width: 35px;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      align-items: flex-end;
      padding-right: 8px;
      font-size: 11px;
      color: var(--uui-color-text);
      opacity: 0.6;
    }

    .chart-container {
      height: 200px;
      display: flex;
      align-items: flex-end;
      gap: 4px;
    }

    .chart-x-axis {
      display: flex;
      justify-content: space-between;
      padding-top: 8px;
      font-size: 10px;
      color: var(--uui-color-text);
      opacity: 0.6;
    }

    .chart-bar {
      flex: 1;
      background: linear-gradient(180deg, oklch(42.4% 0.199 265.638) 0%, oklch(54.6% 0.245 262.881) 100%);
      border-radius: 4px 4px 0 0;
      min-height: 4px;
      transition: height 0.3s ease;
      position: relative;
    }

    .chart-bar:hover {
      background: linear-gradient(180deg, oklch(37.9% 0.146 265.522) 0%, oklch(48.8% 0.243 264.376) 100%);
    }

    .chart-bar:hover::after {
      content: attr(data-count);
      position: absolute;
      top: -24px;
      left: 50%;
      transform: translateX(-50%);
      background: white;
      color: var(--uui-color-text);
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 500;
      white-space: nowrap;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
    }

    .table-section {
      background: white;
      border-radius: 8px;
      padding: 20px;
      margin-bottom: 24px;
      border: 1px solid var(--uui-color-border);
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
    }

    .data-table th {
      text-align: left;
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: var(--uui-color-text);
      opacity: 0.6;
      padding: 12px 16px;
      border-bottom: 1px solid var(--uui-color-border);
    }

    .data-table td {
      padding: 16px;
      font-size: 14px;
      color: var(--uui-color-text);
      border-bottom: 1px solid var(--uui-color-border);
    }

    .data-table tr:last-child td {
      border-bottom: none;
    }

    .data-table tr:hover td {
      background: var(--uui-color-surface);
    }

    .status-badge {
      display: inline-flex;
      align-items: center;
      padding: 4px 10px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
    }

    .status-badge.active {
      background: rgba(16, 185, 129, 0.15);
      color: #10b981;
    }

    .status-badge.quiet {
      background: rgba(107, 114, 128, 0.15);
      color: #6b7280;
    }

    .status-badge.issues {
      background: rgba(239, 68, 68, 0.15);
      color: #ef4444;
    }

    .bottom-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 24px;
    }

    @media (max-width: 900px) {
      .bottom-grid {
        grid-template-columns: 1fr;
      }
    }

    .feed-section,
    .submissions-section {
      background: white;
      border-radius: 8px;
      padding: 20px;
      border: 1px solid var(--uui-color-border);
    }

    .feed-item {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 0;
      border-bottom: 1px solid var(--uui-color-border);
    }

    .feed-item:last-child {
      border-bottom: none;
    }

    .feed-icon {
      width: 32px;
      height: 32px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .feed-icon.success {
      background: rgba(16, 185, 129, 0.15);
      color: #10b981;
    }

    .feed-icon.failure {
      background: rgba(239, 68, 68, 0.15);
      color: #ef4444;
    }

    .feed-content {
      flex: 1;
      min-width: 0;
    }

    .feed-title {
      font-size: 14px;
      font-weight: 500;
      color: var(--uui-color-text);
      margin: 0;
    }

    .feed-meta {
      font-size: 12px;
      color: var(--uui-color-text);
      opacity: 0.6;
      margin-top: 4px;
    }

    .submission-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 0;
      border-bottom: 1px solid var(--uui-color-border);
    }

    .submission-item:last-child {
      border-bottom: none;
    }

    .submission-icon {
      width: 32px;
      height: 32px;
      border-radius: 8px;
      background: rgba(124, 58, 237, 0.15);
      color: oklch(48.8% 0.243 264.376);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .submission-content {
      flex: 1;
      min-width: 0;
    }

    .submission-title {
      font-size: 14px;
      font-weight: 500;
      color: var(--uui-color-text);
      margin: 0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .submission-meta {
      font-size: 12px;
      color: var(--uui-color-text);
      opacity: 0.6;
      margin-top: 4px;
    }

    .empty-state {
      text-align: center;
      padding: 40px 20px;
      color: var(--uui-color-text);
      opacity: 0.6;
    }

    .success-rate-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 6px 12px;
      border-radius: 16px;
      font-size: 14px;
      font-weight: 600;
    }

    .success-rate-badge.high {
      background: rgba(16, 185, 129, 0.15);
      color: #10b981;
    }

    .success-rate-badge.medium {
      background: rgba(245, 158, 11, 0.15);
      color: #f59e0b;
    }

    .success-rate-badge.low {
      background: rgba(239, 68, 68, 0.15);
      color: #ef4444;
    }
  `;
}

export default SproutFormsDashboardElement;
