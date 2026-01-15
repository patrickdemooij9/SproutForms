namespace SproutForms.Umbraco.Core.Models.Attributes
{
    //For the first version, eventually I don't want this dependency on Umbraco stuff
    public class BackofficeFieldAttribute : Attribute
    {
        public string PropertyEditor { get; }

        public BackofficeFieldAttribute(string propertyEditor)
        {
            PropertyEditor = propertyEditor;
        }
    }
}
