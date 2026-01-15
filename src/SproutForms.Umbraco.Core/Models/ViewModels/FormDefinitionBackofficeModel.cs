using SproutForms.Core.Models;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormDefinitionBackofficeModel
    {
        public List<FormRowBackofficeModel> Rows { get; set; } = [];
        public List<FormFieldBackofficeModel> Fields { get; set; } = [];
        public required FormOutcomeBackofficeModel Outcome { get; set; }

        public List<FormWorkflowBackofficeModel> Workflows { get; set; } = [];
    }
}
