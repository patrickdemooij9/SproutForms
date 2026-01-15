using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Builders
{
    public class RowBuilder
    {
        private readonly FormBuilder _form;
        private readonly List<FormColumn> _columns = [];

        internal RowBuilder(FormBuilder form)
        {
            _form = form;
        }

        public RowBuilder Col(int width, Action<ColumnBuilder> configure)
        {
            var column = new ColumnBuilder(_form, width);
            configure(column);
            _columns.Add(column.Build());
            return this;
        }

        internal FormRow Build()
            => new() { Columns = _columns };
    }
}
