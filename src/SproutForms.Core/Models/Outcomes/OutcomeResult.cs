namespace SproutForms.Core.Models.Outcomes
{
    public class OutcomeResult
    {
        public string OutcomeTypeAlias { get; set; } = string.Empty;
        public Dictionary<string, object?> Data { get; set; } = new();
    }
}
