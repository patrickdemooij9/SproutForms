namespace SproutForms.Core.Models
{
    public class Form
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Alias { get; set; }
        public FormSource Source { get; set; }
        public Guid? FolderId { get; set; }

        // Set while the form is in the recycle bin, where it is treated as deleted everywhere but in the bin itself
        public DateTime? TrashedAt { get; set; }
        public string? TrashedBy { get; set; }
        public bool IsTrashed => TrashedAt.HasValue;

        public Form()
        {
            Id = Guid.Empty;
        }
    }
}
