namespace SproutForms.Core.Models
{
    public class FormVersion
    {
        public Guid Id { get; set; }
        public Guid FormId { get; set; }
        public int Version { get; set; }
        public FormStatus Status { get; set; }
        public FormDefinition Definition { get; set; }
        public required string DefinitionHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string CreatedBy { get; set; }
    }
}
