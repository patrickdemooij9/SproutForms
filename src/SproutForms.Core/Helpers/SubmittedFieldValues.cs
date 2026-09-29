using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System.Text.Json;

namespace SproutForms.Core.Helpers
{
    public static class SubmittedFieldValues
    {
        /// <summary>
        /// Only the form's own fields, and no value for a file field: that only comes from an actual upload, never from a posted value.
        /// </summary>
        public static Dictionary<string, JsonElement> Filter(FormVersion formVersion, IEnumerable<KeyValuePair<string, JsonElement>> values)
            => values
                .Where(it => formVersion.Definition.Fields.Any(f => f.Alias == it.Key && f.Configuration is not FileFieldConfig))
                .ToDictionary(it => it.Key, it => it.Value);
    }
}
