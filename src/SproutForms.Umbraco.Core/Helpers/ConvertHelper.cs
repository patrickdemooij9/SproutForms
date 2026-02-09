using System.Text.Json;

namespace SproutForms.Umbraco.Core.Helpers
{
    public static class ConvertHelper
    {
        public static object Convert(object value, Type convertType)
        {
            if (convertType == typeof(Guid) || convertType == typeof(Guid?))
            {
                return Guid.Parse(value.ToString()!);
            }
            if (value is JsonElement jsonElement)
            {
                if (!string.IsNullOrWhiteSpace(jsonElement.ToString()))
                {
                    return jsonElement.Deserialize(convertType);
                }
                return default;
            }
            return System.Convert.ChangeType(value, convertType);
        }
    }
}
