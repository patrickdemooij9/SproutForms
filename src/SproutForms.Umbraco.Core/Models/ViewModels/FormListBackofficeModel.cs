using SproutForms.Core.Models;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormListBackofficeModel
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public int Source { get; set; }
        public int TotalSubmissions { get; set; }
    }
}
