namespace SproutForms.Umbraco.Core.Descriptors.Outcomes
{
    public abstract class BaseOutcomeDescriptor<TConfig> : BaseConfigDescriptor<TConfig>, IOutcomeDescriptor where TConfig : class, new()
    {
        public abstract string OutcomeTypeAlias { get; }

        public abstract string DisplayName { get; }
        public abstract string Description { get; }
    }
}
