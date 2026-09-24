namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    /// <summary>
    /// The form type a form uses, with its settings.
    /// </summary>
    public class FormTypeSelectionBackofficeModel
    {
        public required string TypeAlias { get; set; }
        public required string DisplayName { get; set; }
        public Dictionary<string, object?> Settings { get; set; } = [];
    }
}
