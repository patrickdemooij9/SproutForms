namespace SproutForms.Core.Models.ClientModels
{
    /// <summary>
    /// The texts a front-end shows that aren't part of the form, so it doesn't need its own copy of them.
    /// </summary>
    public sealed class FormClientTexts
    {
        public string Required { get; init; } = FormTexts.Required;
        public string Invalid { get; init; } = FormTexts.Invalid;
        public string SubmitFailed { get; init; } = FormTexts.SubmitFailed;
        public string SubmitSucceeded { get; init; } = FormTexts.SubmitSucceeded;
    }
}
