namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class WorkflowTemplateBackofficeModel
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string WorkflowTypeAlias { get; set; } = string.Empty;
        public string ConfigurationJson { get; set; } = "{}";
        public List<string> LockedFields { get; set; } = [];
    }
}
