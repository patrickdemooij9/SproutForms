using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;

namespace SproutForms.Site.Examples.Quiz
{
    /// <summary>
    /// How the quiz appears in the backoffice: its settings on the Settings tab, and a "Quiz" tab on radio buttons and dropdowns.
    /// </summary>
    public class QuizFormTypeDescriptor : BaseFormDefinitionTypeDescriptor<QuizSettings>
    {
        public override string FormTypeAlias => QuizFormType.TypeAlias;

        public override string DisplayName => "Quiz";
        public override string Description => "Questions with a correct answer and points. Visitors see their score after submitting.";

        public QuizFormTypeDescriptor()
        {
            DefineMap(it => it.PassMark, "passMark", "Pass mark", "Umb.PropertyEditorUi.Integer");
            DefineMap(it => it.PassedMessage, "passedMessage", "Message when passed", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.FailedMessage, "failedMessage", "Message when failed", "Umb.PropertyEditorUi.TextBox");

            // The option picker is a dropdown of the question's own options
            foreach (var fieldTypeAlias in new[] { "radio", "select" })
            {
                ExtendField<QuizAnswerSettings>(fieldTypeAlias, field => field
                    .Map(it => it.CorrectAnswer, "correctAnswer", "Correct answer", "SproutForms.FieldOptionPicker")
                    .Map(it => it.Points, "points", "Points", "Umb.PropertyEditorUi.Integer"));
            }
        }
    }

    public class QuizResultOutcomeDescriptor : BaseOutcomeDescriptor<QuizResultOutcomeConfig>
    {
        public override string OutcomeTypeAlias => QuizResultOutcomeType.TypeAlias;

        public override string DisplayName => "Show quiz score";
        public override string Description => "Shows the visitor their score and whether they passed.";
    }
}
