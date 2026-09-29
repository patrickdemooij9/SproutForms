namespace SproutForms.Core.Models.ClientModels
{
    public sealed class FormClientRow
    {
        public IReadOnlyList<FormClientColumn> Columns { get; init; } = [];
    }
}
