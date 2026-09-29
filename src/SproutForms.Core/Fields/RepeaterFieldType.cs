using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Services;
using System.Text.Json;

namespace SproutForms.Core.Fields
{
    /// <summary>
    /// A group of fields the visitor fills in as often as needed, such as one entry per attendee. Its value is the list of entries;
    /// submitting validates each entry's fields, and this type only checks how many entries there are.
    /// </summary>
    public class RepeaterFieldType : FormFieldBase<RepeaterFieldConfig, List<Dictionary<string, JsonElement>>>
    {
        public override string Alias => "repeater";

        public override RepeaterFieldConfig DefaultConfiguration => new();

        // An entry's fields have labels of their own, so the group is a fieldset with a legend
        public override bool RendersOwnLabel => true;

        protected override IEnumerable<ValidationRule> GetValidationRulesCore(RepeaterFieldConfig config)
        {
            if (config.MinItems is { } min)
            {
                yield return new ValidationRule { Type = "minItems", Value = min, Message = FormTexts.MinItems(min) };
            }

            if (config.MaxItems is { } max)
            {
                yield return new ValidationRule { Type = "maxItems", Value = max, Message = FormTexts.MaxItems(max) };
            }
        }

        protected override object? GetClientConfiguration(RepeaterFieldConfig configuration)
            => new RepeaterFieldClientConfig
            {
                MinItems = configuration.MinItems,
                MaxItems = configuration.MaxItems,
                InitialItems = configuration.GetInitialItemCount(),
                AddLabel = string.IsNullOrWhiteSpace(configuration.AddLabel) ? FormTexts.AddItem : configuration.AddLabel,
                RemoveLabel = string.IsNullOrWhiteSpace(configuration.RemoveLabel) ? FormTexts.RemoveItem : configuration.RemoveLabel,
                ItemTitle = configuration.GetItemTitleTemplate()
            };

        protected override ValidationResult Validate(List<Dictionary<string, JsonElement>> value, RepeaterFieldConfig configuration)
        {
            if (configuration.MinItems is { } min && value.Count < min)
                return ValidationResult.Fail(FormTexts.MinItems(min));

            if (configuration.MaxItems is { } max && value.Count > max)
                return ValidationResult.Fail(FormTexts.MaxItems(max));

            return ValidationResult.Success();
        }

        public override string? GetDisplayValue(JsonElement value, object configuration, FormValueFormatter formatter)
        {
            if (value.ValueKind != JsonValueKind.Array || configuration is not RepeaterFieldConfig config)
                return FormValueFormatter.FormatText(value);

            var fields = GetFieldsInLayoutOrder(config);
            var entries = value.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.Object)
                .Select((entry, index) =>
                {
                    var lines = fields
                        .Select(field => (field.Label, Value: entry.TryGetProperty(field.Alias, out var fieldValue) ? formatter.Format(field, fieldValue) : null))
                        .Where(it => it.Value is not null)
                        .Select(it => $"{it.Label}: {it.Value}");
                    return string.Join("\n", [config.GetItemTitle(index), .. lines]);
                })
                .ToList();

            return entries.Count == 0 ? null : string.Join("\n\n", entries);
        }

        // The order the visitor saw them in; a field the layout doesn't place comes last
        private static List<FormField> GetFieldsInLayoutOrder(RepeaterFieldConfig config)
        {
            var aliases = config.Rows.SelectMany(row => row.Columns).Select(column => column.FieldAlias).ToList();
            return [.. config.Fields.OrderBy(field => aliases.IndexOf(field.Alias) is var index && index >= 0 ? index : int.MaxValue)];
        }
    }
}
