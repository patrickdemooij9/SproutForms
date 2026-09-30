using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Core.Models
{
    public class FormDefinition
    {
        // Definitions stored before form types existed have no type, and read as standard forms
        public FormDefinitionTypeReference Type { get; set; } = FormDefinitionTypeReference.Standard();

        public List<FormPage> Pages { get; set; } = [];
        public List<FormField> Fields { get; set; } = [];
        public List<FormWorkflow> Workflows { get; set; } = [];

        public FormSubmitOutcome SubmitOutcome { get; set; } = FormSubmitOutcome.Default();

        public string? SubmitLabel { get; set; }
        public bool ShowProgress { get; set; } = true;

        /// <summary>
        /// Returns the index of the page that holds the field, or -1 when the field isn't placed on any page.
        /// </summary>
        public int FindPageIndex(string fieldAlias)
            => Pages.FindIndex(page => page.Rows.Any(row => row.Columns.Any(column => column.FieldAlias == fieldAlias)));

        /// <summary>
        /// Every field of the form, with the fields inside field groups such as a repeater right after their group.
        /// </summary>
        public IEnumerable<FormField> GetAllFields() => Flatten(Fields);

        /// <summary>
        /// The field with this alias, at any depth, or null when the form doesn't have it.
        /// </summary>
        public FormField? FindField(string alias) => GetAllFields().FirstOrDefault(field => field.Alias == alias);

        private static IEnumerable<FormField> Flatten(IEnumerable<FormField> fields)
        {
            foreach (var field in fields)
            {
                yield return field;
                if (field.Configuration is IFormFieldGroupConfiguration group)
                {
                    foreach (var child in Flatten(group.Fields))
                        yield return child;
                }
            }
        }
    }
}
