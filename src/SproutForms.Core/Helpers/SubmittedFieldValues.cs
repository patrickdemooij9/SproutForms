using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SproutForms.Core.Helpers
{
    public static class SubmittedFieldValues
    {
        /// <summary>
        /// Only the form's own fields, and no value for a file field: that only comes from an actual upload, never from a posted value.
        /// A field group's value is kept as a list of entries that each hold only the group's own fields.
        /// </summary>
        public static Dictionary<string, JsonElement> Filter(FormVersion formVersion, IEnumerable<KeyValuePair<string, JsonElement>> values)
            => Filter(formVersion.Definition.Fields, values);

        /// <summary>
        /// The values of a posted HTML form. The inputs of a field group's entries are named by their path, such as "people[0].firstName",
        /// and come together in the group's list of entries.
        /// </summary>
        public static Dictionary<string, JsonElement> FromPostedForm(IEnumerable<KeyValuePair<string, string>> values)
        {
            var result = new Dictionary<string, JsonElement>();
            foreach (var (name, value) in values)
            {
                if (FieldPath.TryParse(name, out var path))
                    path.SetValue(result, JsonSerializer.SerializeToElement(value));
            }
            return result;
        }

        /// <summary>
        /// The values of a JSON submission. Validation works on text, as a posted HTML form sends it, so JSON numbers and booleans become their text,
        /// in a field group's entries too; lists and objects stay as they are.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, JsonElement>> FromJson(IReadOnlyDictionary<string, JsonElement> values)
        {
            foreach (var (alias, value) in values)
            {
                var submitted = AsSubmitted(value);
                if (submitted.HasValue)
                    yield return KeyValuePair.Create(alias, submitted.Value);
            }
        }

        private static Dictionary<string, JsonElement> Filter(IReadOnlyList<FormField> fields, IEnumerable<KeyValuePair<string, JsonElement>> values)
        {
            var result = new Dictionary<string, JsonElement>();
            foreach (var (alias, value) in values)
            {
                var field = fields.FirstOrDefault(it => it.Alias == alias);
                var filtered = field is null ? null : FilterValue(field, value);
                if (filtered.HasValue)
                    result[alias] = filtered.Value;
            }
            return result;
        }

        private static JsonElement? FilterValue(FormField field, JsonElement value)
        {
            if (field.Configuration is FileFieldConfig)
                return null;
            if (field.Configuration is not IFormFieldGroupConfiguration group)
                return value;
            if (value.ValueKind != JsonValueKind.Array)
                return null;

            // An entry that isn't an object is kept as an empty one, so the entries after it keep their index for their errors
            var entries = value.EnumerateArray()
                .Select(entry => entry.ValueKind == JsonValueKind.Object
                    ? Filter(group.Fields, entry.EnumerateObject().Select(property => KeyValuePair.Create(property.Name, property.Value)))
                    : [])
                .ToList();
            return JsonSerializer.SerializeToElement(entries);
        }

        private static JsonElement? AsSubmitted(JsonElement value)
            => value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.True => JsonSerializer.SerializeToElement("true"),
                JsonValueKind.False => JsonSerializer.SerializeToElement("false"),
                JsonValueKind.Number => JsonSerializer.SerializeToElement(value.GetRawText()),
                JsonValueKind.Array when value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Object) => AsSubmittedEntries(value),
                _ => value
            };

        // A field group's entries, where each entry's values are taken the same way as the form's
        private static JsonElement AsSubmittedEntries(JsonElement entries)
        {
            var result = new JsonArray();
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    result.Add(new JsonObject());
                    continue;
                }

                var submittedEntry = new JsonObject();
                foreach (var property in entry.EnumerateObject())
                {
                    if (AsSubmitted(property.Value) is { } submitted)
                        submittedEntry[property.Name] = JsonNode.Parse(submitted.GetRawText());
                }
                result.Add(submittedEntry);
            }
            return JsonSerializer.SerializeToElement(result);
        }
    }
}
