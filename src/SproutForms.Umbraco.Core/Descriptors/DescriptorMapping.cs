using System.Linq.Expressions;

namespace SproutForms.Umbraco.Core.Descriptors
{
    public class DescriptorMapping<TConfig>
    {
        public required Expression<Func<TConfig, object?>> Expression { get; set; }

        public required string Alias { get; set; }
        public required string DisplayName { get; set; }
        public required string PropertyTypeAlias { get; set; }

        public Func<object, string>? OverrideFromConfig { get; set; }
        public Func<string, object>? OverrideToConfig { get; set; }
    }
}
