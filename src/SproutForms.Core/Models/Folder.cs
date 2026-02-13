namespace SproutForms.Core.Models
{
    public class Folder
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public Guid? ParentId { get; set; }
        public int SortOrder { get; set; }

        public Folder()
        {
            Id = Guid.Empty;
        }
    }
}
