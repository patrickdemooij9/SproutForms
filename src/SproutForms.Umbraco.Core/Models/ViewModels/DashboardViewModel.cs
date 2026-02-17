using System;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class DashboardViewModel
    {
        public HeroMetricsViewModel HeroMetrics { get; set; } = new();
        public SubmissionTrendViewModel SubmissionTrend { get; set; } = new();
        public FormActivityViewModel FormActivity { get; set; } = new();
        public WorkflowFeedViewModel WorkflowFeed { get; set; } = new();
        public RecentSubmissionsViewModel RecentSubmissions { get; set; } = new();
    }

    public class HeroMetricsViewModel
    {
        public int TotalSubmissionsLast30Days { get; set; }
        public int TotalSubmissionsPrevious30Days { get; set; }
        public double SubmissionsChangePercent { get; set; }
        public int SubmissionsToday { get; set; }
        public double WorkflowSuccessRate { get; set; }
        public int FailedWorkflowsLast7Days { get; set; }
    }

    public class SubmissionTrendViewModel
    {
        public SubmissionTrendDataPoint[] Data { get; set; } = [];
    }

    public class SubmissionTrendDataPoint
    {
        public DateTime Date { get; set; }
        public int SubmissionCount { get; set; }
    }

    public class FormActivityViewModel
    {
        public FormActivityItem[] Forms { get; set; } = [];
    }

    public class FormActivityItem
    {
        public Guid FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public int SubmissionsLast30Days { get; set; }
        public DateTime? LastSubmissionDate { get; set; }
        public int WorkflowFailuresLast30Days { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class WorkflowFeedViewModel
    {
        public WorkflowFeedItem[] Items { get; set; } = [];
    }

    public class WorkflowFeedItem
    {
        public Guid SubmissionId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public string WorkflowType { get; set; } = string.Empty;
        public string WorkflowAlias { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RecentSubmissionsViewModel
    {
        public RecentSubmissionItem[] Items { get; set; } = [];
    }

    public class RecentSubmissionItem
    {
        public Guid SubmissionId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
