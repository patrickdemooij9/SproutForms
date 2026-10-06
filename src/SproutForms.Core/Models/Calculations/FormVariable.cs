namespace SproutForms.Core.Models.Calculations
{
    /// <summary>
    /// A value the form's calculations work out from the answers, such as a quiz score or a price. It is stored with the submission,
    /// and conditions, outcomes and workflows can use it. It stays on the server unless a field's or page's condition uses it.
    /// </summary>
    public class FormVariable
    {
        public required string Alias { get; set; }
        public string? Label { get; set; }
        public FormVariableType Type { get; set; } = FormVariableType.Number;

        // What the variable holds before the first calculation; a number or text, as the type says
        public object? InitialValue { get; set; }

        // How many decimals a number keeps once the calculations are done
        public int Decimals { get; set; }
    }
}
