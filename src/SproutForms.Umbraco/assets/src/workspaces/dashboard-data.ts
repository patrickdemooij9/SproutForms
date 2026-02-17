export interface DashboardData {
  heroMetrics: {
    totalSubmissionsLast30Days: number;
    totalSubmissionsPrevious30Days: number;
    submissionsChangePercent: number;
    submissionsToday: number;
    workflowSuccessRate: number;
    failedWorkflowsLast7Days: number;
  };
  submissionTrend: {
    data: Array<{ date: string; submissionCount: number }>;
  };
  formActivity: {
    forms: Array<{
      formId: string;
      formName: string;
      submissionsLast30Days: number;
      lastSubmissionDate: string | null;
      workflowFailuresLast30Days: number;
      status: string;
    }>;
  };
  workflowFeed: {
    items: Array<{
      submissionId: string;
      formName: string;
      workflowType: string;
      workflowAlias: string;
      success: boolean;
      timestamp: string;
    }>;
  };
  recentSubmissions: Array<{
    submissionId: string;
    formName: string;
    timestamp: string;
  }>;
}

export const mockData: DashboardData = {
  heroMetrics: {
    totalSubmissionsLast30Days: 247,
    totalSubmissionsPrevious30Days: 198,
    submissionsChangePercent: 24.7,
    submissionsToday: 18,
    workflowSuccessRate: 96.4,
    failedWorkflowsLast7Days: 3,
  },
  submissionTrend: {
    data: Array.from({ length: 30 }, (_, i) => {
      const date = new Date();
      date.setDate(date.getDate() - (29 - i));
      return {
        date: date.toISOString().split("T")[0],
        submissionCount: Math.floor(Math.random() * 20) + (i % 7 === 0 ? 5 : 0),
      };
    }),
  },
  formActivity: {
    forms: [
      {
        formId: "1",
        formName: "Contact Form",
        submissionsLast30Days: 124,
        lastSubmissionDate: new Date().toISOString(),
        workflowFailuresLast30Days: 2,
        status: "Active",
      },
      {
        formId: "2",
        formName: "Newsletter Signup",
        submissionsLast30Days: 89,
        lastSubmissionDate: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
        workflowFailuresLast30Days: 0,
        status: "Active",
      },
      {
        formId: "3",
        formName: "Support Request",
        submissionsLast30Days: 34,
        lastSubmissionDate: new Date(Date.now() - 10 * 24 * 60 * 60 * 1000).toISOString(),
        workflowFailuresLast30Days: 5,
        status: "Issues",
      },
      {
        formId: "4",
        formName: "Feedback Form",
        submissionsLast30Days: 0,
        lastSubmissionDate: null,
        workflowFailuresLast30Days: 0,
        status: "Quiet",
      },
    ],
  },
  workflowFeed: {
    items: [
      {
        submissionId: "1",
        formName: "Contact Form",
        workflowType: "Email",
        workflowAlias: "send-notification",
        success: true,
        timestamp: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
      },
      {
        submissionId: "2",
        formName: "Newsletter Signup",
        workflowType: "Email",
        workflowAlias: "subscribe-user",
        success: true,
        timestamp: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
      },
      {
        submissionId: "3",
        formName: "Support Request",
        workflowType: "Email",
        workflowAlias: "notify-support",
        success: false,
        timestamp: new Date(Date.now() - 45 * 60 * 1000).toISOString(),
      },
      {
        submissionId: "4",
        formName: "Contact Form",
        workflowType: "Email",
        workflowAlias: "send-confirmation",
        success: true,
        timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
      },
    ],
  },
  recentSubmissions: [
    {
      submissionId: "1",
      formName: "Contact Form",
      timestamp: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
    },
    {
      submissionId: "2",
      formName: "Newsletter Signup",
      timestamp: new Date(Date.now() - 12 * 60 * 1000).toISOString(),
    },
    {
      submissionId: "3",
      formName: "Contact Form",
      timestamp: new Date(Date.now() - 25 * 60 * 1000).toISOString(),
    },
    {
      submissionId: "4",
      formName: "Support Request",
      timestamp: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(),
    },
    {
      submissionId: "5",
      formName: "Contact Form",
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    },
  ],
};
