using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Site.Examples.Quiz
{
    /// <summary>
    /// Example outcome: shows the visitor their quiz score, read from the results <see cref="QuizFormType"/> stored with the
    /// submission. The "quizResult" handler in wwwroot/examples/form-type-examples.js renders it; without JavaScript the
    /// visitor gets the message.
    /// </summary>
    public class QuizResultOutcomeType : IFormSubmitOutcomeType, IRestrictedToFormTypes
    {
        public const string TypeAlias = "quizResult";

        public string Alias => TypeAlias;

        public Type ConfigurationType => typeof(QuizResultOutcomeConfig);

        // The score only exists in a quiz
        public IReadOnlyCollection<string> FormTypeAliases => [QuizFormType.TypeAlias];

        public object GetDefaultConfiguration() => new QuizResultOutcomeConfig();

        public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken)
        {
            var settings = context.Version.Definition.Type.Settings as QuizSettings ?? new QuizSettings();
            var results = context.Submission.Results;
            var score = results["score"].GetInt32();
            var maxScore = results["maxScore"].GetInt32();
            var passed = results["passed"].GetBoolean();
            var message = passed ? settings.PassedMessage : settings.FailedMessage;

            return Task.FromResult(new OutcomeResult
            {
                Data = new Dictionary<string, object?>
                {
                    ["score"] = score,
                    ["maxScore"] = maxScore,
                    ["passed"] = passed,
                    ["resultMessage"] = message,
                    // For visitors without JavaScript. Written by an editor, never by the visitor: forms.js shows a message as HTML
                    ["message"] = $"{message} You scored {score} of {maxScore}."
                }
            });
        }
    }

    public class QuizResultOutcomeConfig
    {
    }
}
