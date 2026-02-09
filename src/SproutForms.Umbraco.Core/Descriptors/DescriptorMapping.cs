using System.Linq.Expressions;

namespace SproutForms.Umbraco.Core.Descriptors
{
    public class DescriptorMapping<TConfig>
    {
        public required Expression<Func<TConfig, object?>> Expression { get; set; }

        public required string Alias { get; set; }
        public required string DisplayName { get; set; }
        public required string PropertyTypeAlias { get; set; }

        public Func<object, object>? OverrideFromConfig { get; set; }
        public Func<object, object>? OverrideToConfig { get; set; }
    }
}
