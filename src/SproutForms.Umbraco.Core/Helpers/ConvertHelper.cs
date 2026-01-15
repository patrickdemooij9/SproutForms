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
            return System.Convert.ChangeType(value, convertType);
        }
    }
}
