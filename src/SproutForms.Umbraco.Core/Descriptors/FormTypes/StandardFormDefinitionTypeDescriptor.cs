using SproutForms.Core.Models.FormTypes;

namespace SproutForms.Umbraco.Core.Descriptors.FormTypes
{
    public class StandardFormDefinitionTypeDescriptor : BaseFormDefinitionTypeDescriptor<StandardFormDefinitionTypeSettings>
    {
        public override string FormTypeAlias => StandardFormDefinitionType.TypeAlias;

        public override string DisplayName => "Standard form";
        public override string Description => "A form that collects answers, without any special behaviour.";
    }
}
