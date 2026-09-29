using SproutForms.Core.Models;

namespace SproutForms.Core.Fields.Configs
{
    public class RepeaterFieldConfig : IFormFieldGroupConfiguration
    {
        public List<FormField> Fields { get; set; } = [];
        public List<FormRow> Rows { get; set; } = [];

        public int? MinItems { get; set; }
        public int? MaxItems { get; set; }

        // The entries shown before the visitor adds any; without it, one, or as many as MinItems asks for
        public int? InitialItems { get; set; }

        public string? AddLabel { get; set; }
        public string? RemoveLabel { get; set; }

        // The heading of each entry, where {n} is its number, such as "Person {n}"; without it, "Item {n}"
        public string? ItemTitle { get; set; }

        public int GetInitialItemCount()
        {
            var count = Math.Max(InitialItems ?? 1, MinItems ?? 0);
            return MaxItems is { } max ? Math.Min(count, max) : count;
        }

        public string GetItemTitleTemplate() => string.IsNullOrWhiteSpace(ItemTitle) ? FormTexts.ItemTitle : ItemTitle;

        public string GetItemTitle(int index) => GetItemTitleTemplate().Replace("{n}", (index + 1).ToString());
    }
}
