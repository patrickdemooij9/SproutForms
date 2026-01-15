namespace SproutForms.Core.Models.ViewModels
{
    public class AjaxFormResponse
    {
        public bool Success { get; init; }

        public IReadOnlyDictionary<string, List<string>> Errors { get; init; }
            = new Dictionary<string, List<string>>();

        public IReadOnlyDictionary<string, string> Values { get; init; }
            = new Dictionary<string, string>();

        public string? RedirectUrl { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
