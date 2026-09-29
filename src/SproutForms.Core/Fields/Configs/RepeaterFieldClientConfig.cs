namespace SproutForms.Core.Fields.Configs
{
    /// <summary>
    /// What a front-end sees of a <see cref="RepeaterFieldConfig"/>. Its fields are in the client field's rows, each with its own client configuration.
    /// </summary>
    public class RepeaterFieldClientConfig
    {
        public int? MinItems { get; init; }
        public int? MaxItems { get; init; }
        public int InitialItems { get; init; }
        public string AddLabel { get; init; } = string.Empty;
        public string RemoveLabel { get; init; } = string.Empty;
        // The heading of each entry, where {n} is its number
        public string ItemTitle { get; init; } = string.Empty;
    }
}
