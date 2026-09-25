using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormPageBackofficeModel
    {
        public string? Title { get; set; }
        public List<FormRowBackofficeModel> Rows { get; set; } = [];
        public string? NextLabel { get; set; }
        public string? PreviousLabel { get; set; }
        public ConditionDefinition? Visibility { get; set; }

        public FormPageBackofficeModel(FormPage page)
        {
            Title = page.Title;
            Rows = [.. page.Rows.Select(it => new FormRowBackofficeModel(it))];
            NextLabel = page.NextLabel;
            PreviousLabel = page.PreviousLabel;
            Visibility = page.Visibility;
        }

        public FormPageBackofficeModel()
        {
        }
    }
}
