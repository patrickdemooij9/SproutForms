namespace SproutForms.Core.Models
{
    /// <summary>
    /// The configuration of a field type whose field holds fields of its own, laid out in rows like a page, such as a repeater.
    /// The field's value is a list of entries, each an object keyed by the aliases of these fields. They aren't part of
    /// <see cref="FormDefinition.Fields"/>, but their aliases are unique across the whole form.
    /// </summary>
    public interface IFormFieldGroupConfiguration : IFormLayout
    {
        List<FormField> Fields { get; }
    }
}
