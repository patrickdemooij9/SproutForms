using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Builders
{
    public class ColumnBuilder
    {
        internal readonly FormBuilder Form;

        // The fields of the field group this column is in, or null for a column on a page
        internal readonly List<FormField>? GroupFields;

        private string? _fieldAlias;

        public int Width { get; }

        internal ColumnBuilder(FormBuilder form, int width, List<FormField>? groupFields = null)
        {
            Form = form;
            Width = width;
            GroupFields = groupFields;
        }

        internal void SetField(FormField field)
        {
            _fieldAlias = field.Alias;
        }

        internal FormColumn Build()
            => new()
            {
                Width = Width,
                FieldAlias = _fieldAlias
                    ?? throw new InvalidOperationException("Column has no field.")
            };
    }
}
