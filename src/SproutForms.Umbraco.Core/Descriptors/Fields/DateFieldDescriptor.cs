using SproutForms.Core.Fields.Configs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class DateFieldDescriptor : BaseFieldDescriptor<DateFieldConfig>
    {
        public override string FieldTypeAlias => "date";

        public override string DisplayName => "Date";

        public override string Icon => "icon-calendar";

        public DateFieldDescriptor()
        {
            DefineMap(it => it.Min, "min", "Minimum date", "Umb.PropertyEditorUi.Date");
            DefineMap(it => it.Max, "max", "Maximum date", "Umb.PropertyEditorUi.Date");
            DefineMap(it => it.IncludeTime, "includeTime", "Include time", "Umb.PropertyEditorUi.Toggle");
        }
    }
}
