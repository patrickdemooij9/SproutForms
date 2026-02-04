using SproutForms.Core.Fields.Configs;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Text.Json;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class SelectFieldDescriptor : BaseFieldDescriptor<SelectFieldConfig>
    {
        public override string FieldTypeAlias => "select";

        public override string DisplayName => "Dropdown";

        public override string Icon => "icon-list";

        public SelectFieldDescriptor()
        {
            DefineMap(it => it.Options, "options", "Items", "SproutForms.KeyValuePair", (options) =>
            {
                return JsonSerializer.Serialize(((IEnumerable<SelectFieldOption>)options).Select(option => new KeyValuePairModel
                {
                    Key = option.Label,
                    Value = option.Value
                }));
            }, (value) =>
            {
                return JsonSerializer.Deserialize<KeyValuePairModel[]>(value.ToString())?.Select(kvp => new SelectFieldOption
                {
                    Label = kvp.Key,
                    Value = kvp.Value
                }).ToList() ?? [];
            });
        }
    }
}
