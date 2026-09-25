namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormVersionBackofficeModel
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string CreatedByName { get; set; }
        public bool IsCurrent { get; set; }
    }
}
