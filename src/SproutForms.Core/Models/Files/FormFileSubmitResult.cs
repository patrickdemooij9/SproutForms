namespace SproutForms.Core.Models.Files
{
    public class FormFileSubmitResult
    {
        public Dictionary<string, List<string>> Errors { get; set; }
        public Dictionary<string, string> Values { get; set; }

        public FormFileSubmitResult()
        {
            Errors = [];
            Values = [];
        }
    }
}
