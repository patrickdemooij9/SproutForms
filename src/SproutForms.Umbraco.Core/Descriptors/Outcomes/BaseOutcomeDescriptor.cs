using SproutForms.Umbraco.Core.Helpers;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Linq.Expressions;
using System.Reflection;

namespace SproutForms.Umbraco.Core.Descriptors.Outcomes
{
    public abstract class BaseOutcomeDescriptor<TConfig> : IOutcomeDescriptor where TConfig : class, new()
    {
        public abstract string OutcomeTypeAlias { get; }

        public abstract string DisplayName { get; }
        public abstract string Description { get; }


        private List<DescriptorMapping<TConfig>> _mappings = [];

        protected void DefineMap(Expression<Func<TConfig, object?>> expression, string alias, string displayName, string propertyTypeAlias)
        {
            _mappings.Add(new DescriptorMapping<TConfig>
            {
                Expression = expression,
                Alias = alias,
                DisplayName = displayName,
                PropertyTypeAlias = propertyTypeAlias
            });
        }

        public FormPropertyBackofficeModel[] FromConfig(object configuration)
        {
            var items = new List<FormPropertyBackofficeModel>();
            var config = (TConfig)configuration;
            foreach (var mapping in _mappings)
            {
                items.Add(new FormPropertyBackofficeModel
                {
                    Alias = mapping.Alias,
                    DisplayName = mapping.DisplayName,
                    PropertyEditor = mapping.PropertyTypeAlias,
                    Value = mapping.Expression.Compile().Invoke(config)?.ToString()
                });
            }
            return items.ToArray();
        }

        public object ToConfig(Dictionary<string, string> properties)
        {
            var config = new TConfig();
            foreach (var property in properties)
            {
                var mapping = _mappings.FirstOrDefault(it => it.Alias == property.Key);
                if (mapping is null) continue;

                var body = mapping.Expression.Body;

                // Handle UnaryExpression (Convert) that wraps the actual MemberExpression
                if (body is UnaryExpression unaryExpression)
                {
                    body = unaryExpression.Operand;
                }

                if (body is MemberExpression memberSelectorExpression)
                {
                    var configProperty = memberSelectorExpression.Member as PropertyInfo;
                    object value;
                    if (mapping.OverrideToConfig != null)
                    {
                        value = mapping.OverrideToConfig(property.Value);
                    }
                    else
                    {
                        value = ConvertHelper.Convert(property.Value, configProperty.PropertyType);
                    }
                    configProperty?.SetValue(config, value, null);
                }
            }
            return config;
        }
    }
}
