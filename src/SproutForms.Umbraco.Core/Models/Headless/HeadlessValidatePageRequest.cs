using System.Text.Json;

namespace SproutForms.Umbraco.Core.Models.Headless
{
    public sealed class HeadlessValidatePageRequest
    {
        // Keyed by field alias: the values entered so far, from every page, since conditions can depend on earlier pages
        public Dictionary<string, JsonElement> Values { get; init; } = [];
    }
}
