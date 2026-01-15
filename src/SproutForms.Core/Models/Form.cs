namespace SproutForms.Core.Models
{
    public class Form
    {
        public Guid Id { get; set; }
        public required string Name { get; set; } //TODO: Do we want this here or not?
        public required string Alias { get; set; }
        public FormSource Source { get; set; }

        public Form()
        {
            Id = Guid.Empty;
        }
    }
}
