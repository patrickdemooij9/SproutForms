using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Builders
{
    public class ColumnBuilder
    {
        internal readonly FormBuilder Form;
        private string? _fieldAlias;

        public int Width { get; }

        internal ColumnBuilder(FormBuilder form, int width)
        {
            Form = form;
            Width = width;
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
