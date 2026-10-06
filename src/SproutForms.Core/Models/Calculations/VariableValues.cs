using SproutForms.Core.Models.Conditions;
using System.Globalization;
using System.Text.Json;

namespace SproutForms.Core.Models.Calculations
{
    /// <summary>
    /// Converts values for calculations. A value that isn't a number counts as 0, as it does in the browser.
    /// </summary>
    public static class VariableValues
    {
        public const int MaxDecimals = 10;

        public static decimal ToNumber(JsonElement element)
            => element.ValueKind == JsonValueKind.Number
                ? element.TryGetDecimal(out var number) ? number : 0
                : ConditionValues.ParseDecimal(ConditionValues.AsString(element)) ?? 0;

        public static decimal ToNumber(object? value)
            => value switch
            {
                null => 0,
                JsonElement element => ToNumber(element),
                string text => ConditionValues.ParseDecimal(text) ?? 0,
                bool => 0,
                IConvertible convertible => TryConvert(convertible),
                _ => 0
            };

        public static string ToText(JsonElement element) => ConditionValues.AsString(element);

        public static string ToText(object? value)
            => value is JsonElement element ? ToText(element) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

        /// <summary>
        /// A number without trailing zeros, so 10.50 is stored and compared as 10.5, as the browser writes it.
        /// </summary>
        public static JsonElement FromNumber(decimal number)
            => JsonSerializer.SerializeToElement(number / 1.0000000000000000000000000000m);

        public static JsonElement FromText(string text) => JsonSerializer.SerializeToElement(text);

        public static decimal Round(decimal number, int decimals)
            => Math.Round(number, Math.Clamp(decimals, 0, MaxDecimals), MidpointRounding.AwayFromZero);

        /// <summary>
        /// What a variable holds before the first calculation.
        /// </summary>
        public static JsonElement Initial(FormVariable variable)
            => variable.Type == FormVariableType.Number
                ? FromNumber(ToNumber(variable.InitialValue))
                : FromText(ToText(variable.InitialValue));

        private static decimal TryConvert(IConvertible convertible)
        {
            try
            {
                return convertible.ToDecimal(CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is OverflowException or FormatException or InvalidCastException)
            {
                return 0;
            }
        }
    }
}
