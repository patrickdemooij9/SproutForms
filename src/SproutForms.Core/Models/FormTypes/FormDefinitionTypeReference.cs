namespace SproutForms.Core.Models.FormTypes
{
    /// <summary>
    /// The form type a definition uses, with that type's settings for this version of the form.
    /// </summary>
    public class FormDefinitionTypeReference
    {
        public required string TypeAlias { get; set; }
        public required object Settings { get; set; }

        public static FormDefinitionTypeReference Standard()
        {
            return new FormDefinitionTypeReference
            {
                TypeAlias = StandardFormDefinitionType.TypeAlias,
                Settings = new StandardFormDefinitionTypeSettings()
            };
        }
    }
}
