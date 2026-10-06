using SproutForms.Core.Models;
using SproutForms.Core.Models.Calculations;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormDefinitionBackofficeModel
    {
        public required FormTypeSelectionBackofficeModel Type { get; set; }
        public List<FormPageBackofficeModel> Pages { get; set; } = [];
        public List<FormFieldBackofficeModel> Fields { get; set; } = [];
        public required FormOutcomeBackofficeModel Outcome { get; set; }
        public List<FormConditionalOutcomeBackofficeModel> ConditionalOutcomes { get; set; } = [];

        public List<FormVariable> Variables { get; set; } = [];
        public List<CalculationRule> Calculations { get; set; } = [];

        public List<FormWorkflowBackofficeModel> Workflows { get; set; } = [];

        public string? SubmitLabel { get; set; }
        public bool ShowProgress { get; set; } = true;
    }
}
