using SproutForms.Core.Fields;
using SproutForms.Core.Models;
using SproutForms.Core.Models.FormTypes;
using System.Text.Json;

namespace SproutForms.Site.Examples.Quiz
{
    /// <summary>
    /// Example form type: a quiz. Radio buttons and dropdowns get a correct answer and points, and every submission is scored
    /// against the quiz's pass mark. The score is stored with the submission, and <see cref="QuizResultOutcomeType"/> shows it.
    /// </summary>
    public class QuizFormType : FormDefinitionTypeBase<QuizSettings>
    {
        public const string TypeAlias = "quiz";

        public override string Alias => TypeAlias;

        public QuizFormType()
        {
            ExtendField<QuizAnswerSettings>("radio");
            ExtendField<QuizAnswerSettings>("select");
        }

        // An upload can't be scored
        public override bool AllowsFieldType(IFormFieldType fieldType) => fieldType is not FileFieldType;

        public override Task<FormTypeSubmissionResult> ProcessSubmissionAsync(FormTypeSubmissionContext context, CancellationToken cancellationToken)
        {
            var settings = GetSettings(context.Definition);
            var score = 0;
            var maxScore = 0;

            // Questions without a correct answer, such as "What's your name?", don't count
            foreach (var field in context.Definition.Fields.Where(it => it.Extension != null))
            {
                var answer = GetFieldSettings<QuizAnswerSettings>(field);
                if (string.IsNullOrEmpty(answer.CorrectAnswer)) continue;

                maxScore += answer.Points;
                if (context.Values.TryGetValue(field.Alias, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() == answer.CorrectAnswer)
                {
                    score += answer.Points;
                }
            }

            return Task.FromResult(FormTypeSubmissionResult.WithResults(new Dictionary<string, object?>
            {
                ["score"] = score,
                ["maxScore"] = maxScore,
                ["passed"] = score >= settings.PassMark
            }));
        }
    }

    public class QuizSettings
    {
        public int PassMark { get; set; }
        public string PassedMessage { get; set; } = "Well done, you passed!";
        public string FailedMessage { get; set; } = "Not quite, better luck next time.";
    }

    public class QuizAnswerSettings
    {
        // The value of the option that is correct
        public string CorrectAnswer { get; set; } = string.Empty;
        public int Points { get; set; } = 1;
    }
}
