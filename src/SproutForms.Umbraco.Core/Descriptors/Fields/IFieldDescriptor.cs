using SproutForms.Umbraco.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public interface IFieldDescriptor
    {
        string FieldTypeAlias { get; }

        string DisplayName { get; }
        string Icon { get; }

        FormPropertyBackofficeModel[] FromConfig(object configuration);
        object ToConfig(Dictionary<string, string> properties);
    }
}
