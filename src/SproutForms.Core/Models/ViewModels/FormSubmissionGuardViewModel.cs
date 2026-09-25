using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.ViewModels
{
    public class FormSubmissionGuardViewModel
    {
        public string Alias { get; set; }
        public object? Settings { get; set; }

        [JsonIgnore]
        public string? PartialViewPath { get; set; }
    }
}
