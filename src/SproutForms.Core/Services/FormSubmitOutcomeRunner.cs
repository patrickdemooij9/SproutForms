using Microsoft.Extensions.Logging;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Runs a form's submit outcome for a saved submission, for the Razor and the headless endpoints alike.
    /// </summary>
    public class FormSubmitOutcomeRunner
    {
        private readonly IFormSubmitOutcomeType[] _outcomeTypes;
        private readonly IConditionEvaluator _conditionEvaluator;
        private readonly ILogger<FormSubmitOutcomeRunner> _logger;

        public FormSubmitOutcomeRunner(IEnumerable<IFormSubmitOutcomeType> outcomeTypes, IConditionEvaluator conditionEvaluator, ILogger<FormSubmitOutcomeRunner> logger)
        {
            _outcomeTypes = [.. outcomeTypes];
            _conditionEvaluator = conditionEvaluator;
            _logger = logger;
        }

        /// <summary>
        /// Returns null when the outcome type isn't registered or fails. Both are logged, never thrown: the submission is saved at this
        /// point, so a broken outcome must not turn it into an error the visitor would resubmit.
        /// </summary>
        public async Task<OutcomeResult?> RunAsync(FormVersion formVersion, FormSubmission submission, CancellationToken cancellationToken)
        {
            var outcome = GetOutcome(formVersion.Definition, submission);
            var outcomeType = _outcomeTypes.FirstOrDefault(it => it.Alias == outcome.OutcomeTypeAlias);
            if (outcomeType is null)
            {
                _logger.LogError("Form {FormId} uses submit outcome type {OutcomeTypeAlias}, which is not registered", formVersion.FormId, outcome.OutcomeTypeAlias);
                return null;
            }

            try
            {
                var result = await outcomeType.HandleAsync(new FormSubmitOutcomeContext
                {
                    Configuration = outcome.Configuration,
                    Submission = submission,
                    Version = formVersion
                }, cancellationToken);
                result.OutcomeTypeAlias = outcomeType.Alias;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submit outcome type {OutcomeTypeAlias} of form {FormId} failed for submission {SubmissionId}", outcomeType.Alias, formVersion.FormId, submission.Id);
                return null;
            }
        }

        /// <summary>
        /// The first conditional outcome whose condition holds for the submission, or else the form's own submit outcome.
        /// </summary>
        public FormSubmitOutcome GetOutcome(FormDefinition definition, FormSubmission submission)
            => definition.ConditionalOutcomes
                .FirstOrDefault(it => _conditionEvaluator.Evaluate(it.Condition, submission.Values, submission.Variables))?.Outcome
                ?? definition.SubmitOutcome;
    }
}
