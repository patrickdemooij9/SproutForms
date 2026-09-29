namespace SproutForms.Core
{
    /// <summary>
    /// The texts SproutForms uses when a form doesn't set its own.
    /// </summary>
    public static class FormTexts
    {
        public const string Submit = "Submit";
        public const string Next = "Next";
        public const string Previous = "Previous";
        public const string Required = "Field is required.";
        public const string Invalid = "Invalid value.";
        public const string SubmitFailed = "Something went wrong. Please try again.";
        public const string SubmitSucceeded = "Thank you, your submission has been received.";

        public static string Step(int pageIndex) => $"Step {pageIndex + 1}";
    }
}
