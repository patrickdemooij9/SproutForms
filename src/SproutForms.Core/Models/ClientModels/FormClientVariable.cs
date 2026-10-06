using SproutForms.Core.Models.Calculations;

namespace SproutForms.Core.Models.ClientModels
{
    /// <summary>
    /// A variable the browser works out as the visitor answers, because it is exposed or a condition on a field or page needs it.
    /// </summary>
    public sealed class FormClientVariable
    {
        public required string Alias { get; init; }
        public FormVariableType Type { get; init; }
        public object? InitialValue { get; init; }
        public int Decimals { get; init; }
    }
}
