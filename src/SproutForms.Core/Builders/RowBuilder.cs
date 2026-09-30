using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Builders
{
    public class RowBuilder
    {
        private readonly FormBuilder _form;
        private readonly List<FormField>? _groupFields;
        private readonly List<FormColumn> _columns = [];

        internal RowBuilder(FormBuilder form, List<FormField>? groupFields = null)
        {
            _form = form;
            _groupFields = groupFields;
        }

        public RowBuilder Col(int width, Action<ColumnBuilder> configure)
        {
            var column = new ColumnBuilder(_form, width, _groupFields);
            configure(column);
            _columns.Add(column.Build());
            return this;
        }

        internal FormRow Build()
            => new() { Columns = _columns };
    }
}
