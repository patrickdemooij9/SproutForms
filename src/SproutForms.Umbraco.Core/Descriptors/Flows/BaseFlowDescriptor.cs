using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using SproutForms.Umbraco.Core.Helpers;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SproutForms.Umbraco.Core.Descriptors.Flows
{
    public abstract class BaseFlowDescriptor<TConfig> : IFlowDescriptor where TConfig : class, new()
    {
        public abstract string FlowTypeAlias { get; }

        public abstract string DisplayName { get; }
        public abstract string DisplayTemplate { get; }
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
                var value = mapping.Expression.Compile().Invoke(config);

                object? frontendValue = null;
                if (value != null)
                {
                    if (mapping.OverrideFromConfig != null)
                    {
                        frontendValue = mapping.OverrideFromConfig(value);
                    }
                    else
                    {
                        frontendValue = value;
                    }
                }
                items.Add(new FormPropertyBackofficeModel
                {
                    Alias = mapping.Alias,
                    DisplayName = mapping.DisplayName,
                    PropertyEditor = mapping.PropertyTypeAlias,
                    Value = frontendValue
                });
            }
            return items.ToArray();
        }

        public object ToConfig(Dictionary<string, object?> properties)
        {
            var config = new TConfig();
            foreach (var property in properties.Where(it => it.Value != null))
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
                        value = mapping.OverrideToConfig(property.Value!);
                    }
                    else
                    {
                        value = ConvertHelper.Convert(property.Value!, configProperty.PropertyType);
                    }
                    configProperty?.SetValue(config, value, null);
                }
            }
            return config;
        }
    }
}
