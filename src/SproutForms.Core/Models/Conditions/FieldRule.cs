namespace SproutForms.Core.Models.Conditions
{
    /// <summary>
    /// What happens to a field when a condition holds. A field is hidden while any of its Hide rules holds and, when it has Show rules,
    /// only shown while one of them holds; it is required while any of its Require rules holds.
    /// </summary>
    public class FieldRule
    {
        // No condition, or one without rules, always holds
        public ConditionDefinition Condition { get; set; } = new();
        public FieldRuleAction Action { get; set; } = FieldRuleAction.Show;
    }
}
