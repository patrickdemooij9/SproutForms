namespace SproutForms.Core.Models
{
    public interface IFormTypeRequiredHandler
    {
        ValidationResult CheckForRequired(string value);
    }
}
