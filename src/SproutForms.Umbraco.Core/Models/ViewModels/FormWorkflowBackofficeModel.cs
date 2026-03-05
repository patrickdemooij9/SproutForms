namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormWorkflowBackofficeModel
    {
        public string Alias { get; set; }
        public string TypeAlias { get; set; }
        public string DisplayName { get; set; }
        public int Order { get; set; }
        public Dictionary<string, object?> Configuration { get; set; }
        public Guid? TemplateId { get; set; }
    }
}
