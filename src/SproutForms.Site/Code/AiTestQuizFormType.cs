using SproutForms.Core.Fields;
using SproutForms.Core.Models;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.FormTypes;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Form type for the verify-in-site flow: a quiz with a pass mark, which gives radio fields a correct answer and points,
    /// and doesn't allow file uploads.
    /// </summary>
    public class AiTestQuizFormType : FormDefinitionTypeBase<AiTestQuizSettings>
    {
        public const string TypeAlias = "aiTestQuiz";

        public override string Alias => TypeAlias;

        public AiTestQuizFormType()
        {
            ExtendField<AiTestQuizAnswerSettings>("radio");
        }

        public override bool AllowsFieldType(IFormFieldType fieldType) => fieldType is not FileFieldType;
    }

    public class AiTestQuizSettings
    {
        public int PassMark { get; set; }
    }

    public class AiTestQuizAnswerSettings
    {
        public string CorrectAnswer { get; set; } = string.Empty;
        public int Points { get; set; } = 1;
    }

    public class AiTestQuizFormTypeDescriptor : BaseFormDefinitionTypeDescriptor<AiTestQuizSettings>
    {
        public override string FormTypeAlias => AiTestQuizFormType.TypeAlias;

        public override string DisplayName => "AI test quiz";
        public override string Description => "A quiz: radio buttons get a correct answer and points. File uploads aren't allowed.";

        public AiTestQuizFormTypeDescriptor()
        {
            DefineMap(it => it.PassMark, "passMark", "Pass mark", "Umb.PropertyEditorUi.Integer");

            ExtendField<AiTestQuizAnswerSettings>("radio", field => field
                .Map(it => it.CorrectAnswer, "correctAnswer", "Correct answer", "SproutForms.FieldOptionPicker")
                .Map(it => it.Points, "points", "Points", "Umb.PropertyEditorUi.Integer"));
        }
    }
}
