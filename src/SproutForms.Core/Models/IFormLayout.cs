namespace SproutForms.Core.Models
{
    /// <summary>
    /// Rows of columns that each place a field by its alias: a page of the form, or an entry of a field group such as a repeater.
    /// </summary>
    public interface IFormLayout
    {
        List<FormRow> Rows { get; }
    }
}
