namespace SproutForms.Core.Models.FormTypes
{
    public class StandardFormDefinitionType : FormDefinitionTypeBase<StandardFormDefinitionTypeSettings>
    {
        public const string TypeAlias = "standard";

        public override string Alias => TypeAlias;
    }

    public class StandardFormDefinitionTypeSettings
    {
    }
}
