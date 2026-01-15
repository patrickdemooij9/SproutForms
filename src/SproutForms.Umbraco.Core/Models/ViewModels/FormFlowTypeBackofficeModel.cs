namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormFlowTypeBackofficeModel
    {
        public string Alias { get; set; }
        public string DisplayName { get; set; }
        public FormPropertyBackofficeModel[] Configuration { get; set; }
    }
}
