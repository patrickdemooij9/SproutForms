using SproutForms.Core.Models;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormDefinitionBackofficeModel
    {
        public required FormTypeSelectionBackofficeModel Type { get; set; }
        public List<FormPageBackofficeModel> Pages { get; set; } = [];
        public List<FormFieldBackofficeModel> Fields { get; set; } = [];
        public required FormOutcomeBackofficeModel Outcome { get; set; }

        public List<FormWorkflowBackofficeModel> Workflows { get; set; } = [];

        public string? SubmitLabel { get; set; }
        public bool ShowProgress { get; set; } = true;
    }
}
