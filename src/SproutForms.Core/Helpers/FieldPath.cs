using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SproutForms.Core.Helpers
{
    /// <summary>
    /// Where a submitted value goes: a field's alias, or a field in an entry of a field group, such as "people[0].firstName".
    /// Posted HTML inputs and uploads are named by it, and errors are keyed by it.
    /// </summary>
    public sealed class FieldPath
    {
        // More entries than any form asks for; it keeps a posted name from making the server build a huge list
        public const int MaxEntryIndex = 999;

        private static readonly Regex EntrySegment = new(@"^(?<alias>[^.\[\]]+)\[(?<index>\d{1,3})\]$", RegexOptions.Compiled);
        private static readonly Regex FieldSegment = new(@"^[^.\[\]]+$", RegexOptions.Compiled);

        // Every segment but the last is a field group's entry; the last is the field
        public IReadOnlyList<(string Alias, int Index)> Entries { get; }
        public string FieldAlias { get; }

        private FieldPath(IReadOnlyList<(string Alias, int Index)> entries, string fieldAlias)
        {
            Entries = entries;
            FieldAlias = fieldAlias;
        }

        public bool IsNested => Entries.Count > 0;

        public static bool TryParse(string value, [NotNullWhen(true)] out FieldPath? path)
        {
            path = null;
            var segments = value.Split('.');
            if (!FieldSegment.IsMatch(segments[^1]))
                return false;

            var entries = new List<(string, int)>();
            foreach (var segment in segments[..^1])
            {
                var match = EntrySegment.Match(segment);
                if (!match.Success)
                    return false;

                var index = int.Parse(match.Groups["index"].Value);
                if (index > MaxEntryIndex)
                    return false;

                entries.Add((match.Groups["alias"].Value, index));
            }

            path = new FieldPath(entries, segments[^1]);
            return true;
        }

        public override string ToString()
            => string.Concat(Entries.Select(entry => $"{entry.Alias}[{entry.Index}].")) + FieldAlias;

        public static string ForEntry(string prefix, int index) => $"{prefix}[{index}].";

        /// <summary>
        /// Puts the value at this path, adding the entries on the way that aren't there yet.
        /// </summary>
        public void SetValue(Dictionary<string, JsonElement> values, JsonElement value)
        {
            if (!IsNested)
            {
                values[FieldAlias] = value;
                return;
            }

            var (alias, index) = Entries[0];
            var entries = values.TryGetValue(alias, out var existing) && existing.ValueKind == JsonValueKind.Array
                ? JsonNode.Parse(existing.GetRawText())!.AsArray()
                : [];
            SetInEntries(entries, index, 1, value);
            values[alias] = JsonSerializer.SerializeToElement(entries);
        }

        private void SetInEntries(JsonArray entries, int index, int depth, JsonElement value)
        {
            while (entries.Count <= index)
                entries.Add(new JsonObject());

            if (entries[index] is not JsonObject entry)
            {
                entry = [];
                entries[index] = entry;
            }

            if (depth == Entries.Count)
            {
                entry[FieldAlias] = JsonNode.Parse(value.GetRawText());
                return;
            }

            var (alias, childIndex) = Entries[depth];
            if (entry[alias] is not JsonArray children)
            {
                children = [];
                entry[alias] = children;
            }
            SetInEntries(children, childIndex, depth + 1, value);
        }
    }
}
