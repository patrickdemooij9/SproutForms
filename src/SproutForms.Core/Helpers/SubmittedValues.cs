using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using System.Text.Json;

namespace SproutForms.Core.Helpers
{
    /// <summary>
    /// The values of the form, or of one entry of a field group, while a submission is processed. It only holds its own fields: a field group's
    /// value is its entries, a value posted for an upload is never taken, and JSON numbers and booleans are text, as a posted HTML form sends them.
    /// </summary>
    internal sealed class SubmittedValues
    {
        // A field's value, or a field group's entries, in the order they were submitted
        private readonly Dictionary<string, (JsonElement Value, List<SubmittedValues>? Entries)> _values = [];

        /// <summary>
        /// Takes what the visitor submitted, keeping only the values the fields can hold. This is the only place that looks at the shape
        /// of submitted JSON; everything after it can rely on a field group's value being its entries.
        /// </summary>
        public static SubmittedValues Parse(IReadOnlyList<FormField> fields, IEnumerable<KeyValuePair<string, JsonElement>> values)
        {
            var result = new SubmittedValues();
            foreach (var (alias, value) in values)
            {
                var field = fields.FirstOrDefault(it => it.Alias == alias);
                if (field is null || field.Configuration is FileFieldConfig)
                    continue;

                if (field.Configuration is IFormFieldGroupConfiguration group)
                {
                    // An entry that isn't an object is kept as an empty one, so the entries after it keep their index for their errors
                    if (value.ValueKind == JsonValueKind.Array)
                        result.SetEntries(alias, [.. value.EnumerateArray().Select(entry => entry.ValueKind == JsonValueKind.Object
                            ? Parse(group.Fields, entry.EnumerateObject().Select(property => KeyValuePair.Create(property.Name, property.Value)))
                            : new SubmittedValues())]);
                }
                else if (AsText(value) is { } text)
                {
                    result.SetValue(alias, text);
                }
            }
            return result;
        }

        public bool TryGetValue(string alias, out JsonElement value)
        {
            var found = _values.TryGetValue(alias, out var item) && item.Entries is null;
            value = found ? item.Value : default;
            return found;
        }

        public void SetValue(string alias, JsonElement value) => _values[alias] = (value, null);

        public List<SubmittedValues> GetEntries(string alias)
            => _values.TryGetValue(alias, out var item) && item.Entries is { } entries ? entries : [];

        public void SetEntries(string alias, List<SubmittedValues> entries) => _values[alias] = (default, entries);

        // The entry at the index, with the entries before it added when they weren't submitted
        public SubmittedValues GetOrAddEntry(string alias, int index)
        {
            var entries = GetEntries(alias);
            while (entries.Count <= index)
                entries.Add(new SubmittedValues());
            SetEntries(alias, entries);
            return entries[index];
        }

        /// <summary>
        /// The values as they are stored, and as conditions and form types see them.
        /// </summary>
        public Dictionary<string, JsonElement> ToDictionary()
            => _values.ToDictionary(it => it.Key, it => it.Value.Entries is { } entries
                ? JsonSerializer.SerializeToElement(entries.Select(entry => entry.ToDictionary()))
                : it.Value.Value);

        private static JsonElement? AsText(JsonElement value)
            => value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.True => JsonSerializer.SerializeToElement("true"),
                JsonValueKind.False => JsonSerializer.SerializeToElement("false"),
                JsonValueKind.Number => JsonSerializer.SerializeToElement(value.GetRawText()),
                _ => value
            };
    }
}
