namespace SproutForms.Core.Models.FormTypes
{
    /// <summary>
    /// Implemented by a field or outcome type that only makes sense in certain form types, such as a quiz answer field.
    /// Types without it are allowed in every form type that doesn't exclude them.
    /// </summary>
    public interface IRestrictedToFormTypes
    {
        IReadOnlyCollection<string> FormTypeAliases { get; }
    }
}
