namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class TrashedFormBackofficeModel
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Alias { get; set; }
        public DateTime TrashedAt { get; set; }
        public required string TrashedByName { get; set; }

        // The folder a restore puts the form back in; null for the root, or when the folder no longer exists
        public string? FolderName { get; set; }
        public int TotalSubmissions { get; set; }
    }
}
