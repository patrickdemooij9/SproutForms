using SproutForms.Core.Fields.Configs;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class RadioFieldDescriptor : BaseFieldDescriptor<RadioFieldConfig>
    {
        public override string FieldTypeAlias => "radio";

        public override string DisplayName => "Radio buttons";

        public override string Icon => "icon-thumbnail-list";

        public RadioFieldDescriptor()
        {
            DefineMap(it => it.Options, "options", "Items", "SproutForms.KeyValuePair", (options) =>
            {
                return JsonSerializer.Serialize(((IEnumerable<RadioFieldOption>)options).Select(option => new KeyValuePairModel
                {
                    Key = option.Label,
                    Value = option.Value
                }));
            }, (value) =>
            {
                return JsonSerializer.Deserialize<KeyValuePairModel[]>(value)!.Select(kvp => new RadioFieldOption
                {
                    Label = kvp.Key,
                    Value = kvp.Value
                }).ToList();
            });
        }
    }
}
