using SproutForms.Core.Models;
using System.Text.Json;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Shows submitted values as text, for workflows and the backoffice, the way each field's type shows its value.
    /// </summary>
    public class FormValueFormatter
    {
        private readonly IFormFieldType[] _fieldTypes;

        public FormValueFormatter(IEnumerable<IFormFieldType> fieldTypes)
        {
            _fieldTypes = [.. fieldTypes];
        }

        /// <summary>
        /// The text of a field's value, or null when it was left empty. A value can outlive its field when the stored submission predates the
        /// current definition, so without a field, or with a field type that is no longer registered, the value shows as it was stored.
        /// </summary>
        public string? Format(FormField? field, JsonElement value)
        {
            var fieldType = field is null ? null : _fieldTypes.FirstOrDefault(it => it.Alias == field.FieldTypeAlias);
            return fieldType is null
                ? FormatText(value)
                : fieldType.GetDisplayValue(value, field!.Configuration, this);
        }

        /// <summary>
        /// The label and text of each value of a submission that isn't empty, in the order of the submission.
        /// </summary>
        public IEnumerable<(string Label, string Value)> FormatAll(IReadOnlyDictionary<string, JsonElement> values, IReadOnlyList<FormField> fields)
        {
            foreach (var (alias, value) in values)
            {
                var field = fields.FirstOrDefault(it => it.Alias == alias);
                var text = Format(field, value);
                if (text is not null)
                    yield return (field?.Label ?? alias, text);
            }
        }

        /// <summary>
        /// A value as it was submitted: its text, or the JSON of anything that isn't text.
        /// </summary>
        public static string? FormatText(JsonElement value)
        {
            var text = value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => value.GetString(),
                _ => value.GetRawText()
            };

            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
