using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public abstract class FormFieldBase<TConfig, TValue>: IFormFieldType where TConfig: class
    {
        public abstract Guid Id { get; }

        public abstract string Alias { get; }

        public abstract string DisplayName { get; }

        public Type ConfigurationType => typeof(TConfig);

        public abstract TConfig DefaultConfiguration { get; }

        public virtual bool RendersOwnLabel => false;

        object IFormFieldType.DefaultConfiguration => DefaultConfiguration;

        public ValidationResult Validate(JsonElement value, object configuration)
        {
            var typedValue = ConvertValue(value);

            return Validate(typedValue, (TConfig) configuration);
        }

        protected abstract ValidationResult Validate(TValue value, TConfig configuration);

        private static TValue ConvertValue(JsonElement value)
        {
            if (typeof(TValue) == typeof(string))
                return (TValue)(object)value.GetString()!;

            var valueAsString = value.ToString();
            if (valueAsString.StartsWith('{'))
            {
                return JsonSerializer.Deserialize<TValue>(valueAsString)!;
            }

            return Convert.ChangeType(value.ToString(), typeof(TValue)) is TValue convertedValue
                ? convertedValue
                : throw new InvalidOperationException($"Unsupported value type {typeof(TValue)}");
        }
    }
}
