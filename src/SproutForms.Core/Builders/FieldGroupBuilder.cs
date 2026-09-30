using SproutForms.Core.Models;

namespace SproutForms.Core.Builders
{
    /// <summary>
    /// Lays out the fields of a field group, such as a repeater, in rows like a page.
    /// </summary>
    public class FieldGroupBuilder
    {
        private readonly FormBuilder _form;
        private readonly IFormFieldGroupConfiguration _group;

        internal FieldGroupBuilder(FormBuilder form, IFormFieldGroupConfiguration group)
        {
            _form = form;
            _group = group;
        }

        public FieldGroupBuilder Row(Action<RowBuilder> configure)
        {
            var row = new RowBuilder(_form, _group.Fields);
            configure(row);
            _group.Rows.Add(row.Build());
            return this;
        }
    }
}
