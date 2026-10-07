using NSubstitute;
using SproutForms.Core.Flows;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Tests
{
    public class WorkflowRunnerTests
    {
        private IWorkflowExecutionRepository _executions = default!;
        private IFormWorkflowType _workflowType = default!;
        private WorkflowRunner _runner = default!;

        [SetUp]
        public void SetUp()
        {
            _executions = Substitute.For<IWorkflowExecutionRepository>();
            _workflowType = Substitute.For<IFormWorkflowType>();
            _workflowType.Alias.Returns("test");
            _workflowType.ConfigurationType.Returns(typeof(object));
            _workflowType.ExecuteAsync(Arg.Any<WorkflowContext>(), Arg.Any<CancellationToken>()).Returns(new WorkflowExecutionResult(true));

            var submissions = Substitute.For<IFormSubmissionRepository>();
            submissions.Get(Arg.Any<Guid>()).Returns(new FormSubmission { Id = Guid.NewGuid(), FormVersionId = Guid.NewGuid() });

            _runner = new WorkflowRunner(_executions, Substitute.For<IFormVersionRepository>(), submissions, [_workflowType]);
        }

        [Test]
        public async Task An_execution_claimed_elsewhere_is_not_run()
        {
            var execution = Execution(WorkflowExecutionStatus.Retrying, attemptCount: 2);
            _executions.TrySaveExecution(execution, WorkflowExecutionStatus.Retrying, 2).Returns(false);

            var ran = await _runner.ExecuteWorkflowAsync(execution, CancellationToken.None);

            Assert.That(ran, Is.False);
            await _workflowType.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
            await _executions.DidNotReceiveWithAnyArgs().SaveExecution(default!);
        }

        [Test]
        public async Task A_claimed_execution_runs_and_saves_its_result()
        {
            var execution = Execution(WorkflowExecutionStatus.Pending, attemptCount: 0);
            _executions.TrySaveExecution(execution, WorkflowExecutionStatus.Pending, 0).Returns(true);

            var ran = await _runner.ExecuteWorkflowAsync(execution, CancellationToken.None);

            Assert.That(ran, Is.True);
            Assert.That(execution.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(execution.AttemptCount, Is.EqualTo(1));
            await _workflowType.ReceivedWithAnyArgs(1).ExecuteAsync(default!, default);
            await _executions.Received(1).SaveExecution(execution);
        }

        [Test]
        public async Task A_retry_that_loses_to_the_worker_is_not_queued()
        {
            var execution = Execution(WorkflowExecutionStatus.Retrying, attemptCount: 3);
            _executions.GetBySubmissionId(execution.SubmissionId).Returns([execution]);
            _executions.TrySaveExecution(execution, WorkflowExecutionStatus.Retrying, 3).Returns(false);

            var result = await _runner.RetryAsync(execution.SubmissionId, execution.WorkflowAlias);

            Assert.That(result, Is.EqualTo(WorkflowRetryResult.NotRetryable));
        }

        [Test]
        public async Task A_retry_resets_the_attempts_of_a_failed_execution()
        {
            var execution = Execution(WorkflowExecutionStatus.Failed, attemptCount: 5);
            _executions.GetBySubmissionId(execution.SubmissionId).Returns([execution]);
            _executions.TrySaveExecution(execution, WorkflowExecutionStatus.Failed, 5).Returns(true);

            var result = await _runner.RetryAsync(execution.SubmissionId, execution.WorkflowAlias);

            Assert.That(result, Is.EqualTo(WorkflowRetryResult.Queued));
            Assert.That(execution.Status, Is.EqualTo(WorkflowExecutionStatus.Pending));
            Assert.That(execution.AttemptCount, Is.Zero);
        }

        private static WorkflowExecution Execution(WorkflowExecutionStatus status, int attemptCount) => new()
        {
            Id = Guid.NewGuid(),
            SubmissionId = Guid.NewGuid(),
            WorkflowAlias = "notify",
            WorkflowTypeAlias = "test",
            ConfigurationJson = "{}",
            Status = status,
            AttemptCount = attemptCount
        };
    }
}
