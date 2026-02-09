using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models.Files;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class FileFieldDescriptor : BaseFieldDescriptor<FileFieldConfig>
    {
        public override string FieldTypeAlias => "file";

        public override string DisplayName => "File";

        public override string Icon => "icon-document-user";

        public FileFieldDescriptor()
        {
            DefineMap(it => it.MaxFileSizeBytes, "maxFileSizeBytes", "Max file size bytes", "Umb.PropertyEditorUi.Integer", (value) => value.ToString(), (value) => long.Parse(value.ToString()));
            DefineMap(it => it.AllowedExtensions, "allowedExtensions", "Allowed extensions", "Umb.PropertyEditorUi.MultipleTextString");
            DefineMap(it => it.StorageProviderAlias, "storageProviderAlias", "Storage provider", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
