using Microsoft.Extensions.Caching.Memory;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using SproutForms.Umbraco.Core.Models.ViewModels;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Services
{
    public class SproutFormsDashboardService : ISproutFormsDashboardService
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IFormRepository _formRepository;
        private readonly IFormVersionRepository _formVersionRepository;

        public SproutFormsDashboardService(IScopeProvider scopeProvider, IFormRepository formRepository, IFormVersionRepository formVersionRepository)
        {
            _scopeProvider = scopeProvider;
            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
        }

        public DashboardViewModel GetDashboard()
        {
            return BuildDashboard();
        }

        private DashboardViewModel BuildDashboard()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var thirtyDaysAgo = today.AddDays(-30);
            var sixtyDaysAgo = today.AddDays(-60);
            var fourteenDaysAgo = today.AddDays(-14);
            var sevenDaysAgo = today.AddDays(-7);

            using var scope = _scopeProvider.CreateScope(autoComplete: true);

            var heroMetrics = new HeroMetricsViewModel();

            var submissionsLast60Days = scope.Database.Fetch<FormSubmissionEntity>(
                scope.SqlContext.Sql()
                .SelectAll().From<FormSubmissionEntity>().Where<FormSubmissionEntity>(it => it.SubmittedAt >= sixtyDaysAgo));
            var submissionsLast30Days = submissionsLast60Days.Where(it => it.SubmittedAt >= thirtyDaysAgo).ToArray();
            heroMetrics.TotalSubmissionsLast30Days = submissionsLast30Days.Length;
            heroMetrics.TotalSubmissionsPrevious30Days = submissionsLast60Days.Count(it => it.SubmittedAt < thirtyDaysAgo);

            if (heroMetrics.TotalSubmissionsPrevious30Days > 0)
            {
                heroMetrics.SubmissionsChangePercent = Math.Round(
                    ((double)(heroMetrics.TotalSubmissionsLast30Days - heroMetrics.TotalSubmissionsPrevious30Days)
                     / heroMetrics.TotalSubmissionsPrevious30Days) * 100, 1);
            }

            heroMetrics.SubmissionsToday = submissionsLast60Days.Count(s => s.SubmittedAt >= today);

            var workflowsLast30Days = scope.Database.Fetch<WorkflowExecutionEntity>(scope.SqlContext.Sql()
                .SelectAll().From<WorkflowExecutionEntity>().Where<WorkflowExecutionEntity>(it => it.CreatedUtc >= thirtyDaysAgo));

            var successfulWorkflows = workflowsLast30Days.Count(w => w.Status == (int)WorkflowExecutionStatus.Succeeded);
            var totalWorkflows = workflowsLast30Days.Count;
            heroMetrics.WorkflowSuccessRate = totalWorkflows > 0
                ? Math.Round((double)successfulWorkflows / totalWorkflows * 100, 1)
                : 100.0;

            heroMetrics.FailedWorkflowsLast7Days = workflowsLast30Days.Count(w => w.CreatedUtc >= sevenDaysAgo && w.Status == (int)WorkflowExecutionStatus.Failed);

            var submissionTrend = BuildSubmissionTrend(submissionsLast30Days, thirtyDaysAgo, today);

            var forms = _formRepository.Get(0, int.MaxValue, out _);
            var formLatestVersions = forms.ToDictionary(it => it.Id, it => _formVersionRepository.GetLatest(it.Id)); //TODO: Performance will be horrible on big sites here

            var formActivity = new FormActivityViewModel
            {
                Forms = forms.Select(f =>
                {
                    var latestVersion = formLatestVersions[f.Id];

                    var formSubmissions = submissionsLast30Days
                        .Where(s => latestVersion != null && s.FormVersionId == latestVersion.Id)
                        .ToList();

                    var formWorkflows = workflowsLast30Days
                        .Where(w => latestVersion != null && submissionsLast30Days.Any(s => s.Id == w.SubmissionId && s.FormVersionId == latestVersion.Id))
                        .ToList();

                    var formWorkflowFailures = formWorkflows.Count(w => w.Status == (int)WorkflowExecutionStatus.Failed);

                    var lastSubmission = formSubmissions.OrderByDescending(s => s.SubmittedAt).FirstOrDefault();
                    var hasNoSubmission14Days = lastSubmission == null || lastSubmission.SubmittedAt < sevenDaysAgo;
                    var failureRate = formWorkflows.Count > 0
                        ? (double)formWorkflowFailures / formWorkflows.Count * 100
                        : 0;

                    string status;
                    if (failureRate > 20)
                        status = "Issues";
                    else if (hasNoSubmission14Days)
                        status = "Quiet";
                    else
                        status = "Active";

                    return new FormActivityItem
                    {
                        FormId = f.Id,
                        FormName = f.Name,
                        SubmissionsLast30Days = formSubmissions.Count,
                        LastSubmissionDate = lastSubmission?.SubmittedAt,
                        WorkflowFailuresLast30Days = formWorkflowFailures,
                        Status = status
                    };
                }).ToArray()
            };

            var latestWorkflowExecutions = scope.Database.Fetch<WorkflowExecutionEntity>(
                scope.SqlContext.Sql()
                .SelectAll()
                .From<WorkflowExecutionEntity>()
                .OrderByDescending<WorkflowExecutionEntity>(it => it.CreatedUtc)
                .SelectTop(5));

            var workflowFeed = new WorkflowFeedViewModel
            {
                Items = latestWorkflowExecutions.Select(w =>
                {
                    var submission = submissionsLast30Days.FirstOrDefault(s => s.Id == w.SubmissionId);
                    if (submission is null) return null;
                    var version = _formVersionRepository.Get(submission.FormVersionId);
                    var form = forms.FirstOrDefault(f => version != null && f.Id == version.FormId);

                    return new WorkflowFeedItem
                    {
                        SubmissionId = w.SubmissionId,
                        FormName = form?.Name ?? "Unknown Form",
                        WorkflowType = w.WorkflowTypeAlias,
                        WorkflowAlias = w.WorkflowAlias,
                        Success = w.Status == (int)WorkflowExecutionStatus.Succeeded,
                        Timestamp = w.CreatedUtc
                    };
                }).WhereNotNull().ToArray()
            };

            var recentSubmissions = scope.Database.Fetch<FormSubmissionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormSubmissionEntity>()
                .OrderByDescending<FormSubmissionEntity>(it => it.SubmittedAt)
                .SelectTop(5));

            var recentSubmissionsVm = new RecentSubmissionsViewModel
            {
                Items = recentSubmissions.Select(s =>
                {
                    var version = _formVersionRepository.Get(s.FormVersionId);
                    var form = forms.FirstOrDefault(f => version != null && f.Id == version.FormId);

                    return new RecentSubmissionItem
                    {
                        SubmissionId = s.Id,
                        FormName = form?.Name ?? "Unknown Form",
                        Timestamp = s.SubmittedAt
                    };
                }).ToArray()
            };

            return new DashboardViewModel
            {
                HeroMetrics = heroMetrics,
                SubmissionTrend = submissionTrend,
                FormActivity = formActivity,
                WorkflowFeed = workflowFeed,
                RecentSubmissions = recentSubmissionsVm
            };
        }

        private SubmissionTrendViewModel BuildSubmissionTrend(
            IEnumerable<FormSubmissionEntity> submissions,
            DateTime thirtyDaysAgo,
            DateTime today)
        {
            var dailyCounts = submissions
                .GroupBy(s => s.SubmittedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var dataPoints = new List<SubmissionTrendDataPoint>();
            for (var date = thirtyDaysAgo; date <= today; date = date.AddDays(1))
            {
                dataPoints.Add(new SubmissionTrendDataPoint
                {
                    Date = date,
                    SubmissionCount = dailyCounts.GetValueOrDefault(date, 0)
                });
            }

            return new SubmissionTrendViewModel { Data = dataPoints.ToArray() };
        }
    }
}
