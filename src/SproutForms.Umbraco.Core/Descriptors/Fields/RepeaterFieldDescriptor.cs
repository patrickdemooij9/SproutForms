using SproutForms.Core.Fields.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    // The repeater's own fields and their layout are edited on the canvas, not as settings
    public class RepeaterFieldDescriptor : BaseFieldDescriptor<RepeaterFieldConfig>
    {
        public override string FieldTypeAlias => "repeater";

        public override string DisplayName => "Repeatable block";

        public override string Icon => "icon-layers";

        public RepeaterFieldDescriptor()
        {
            DefineMap(it => it.MinItems, "minItems", "Minimum entries", "Umb.PropertyEditorUi.Integer");
            DefineMap(it => it.MaxItems, "maxItems", "Maximum entries", "Umb.PropertyEditorUi.Integer");
            DefineMap(it => it.InitialItems, "initialItems", "Entries shown at first", "Umb.PropertyEditorUi.Integer");
            DefineMap(it => it.ItemTitle, "itemTitle", "Entry title ({n} is its number)", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.AddLabel, "addLabel", "Add button label", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.RemoveLabel, "removeLabel", "Remove button label", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
