using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;

namespace SproutForms.Core.Fields
{
    public class FileFieldType : FormFieldBase<FileFieldConfig, StoredFileReference>
    {
        public override string Alias => "file";

        public override FileFieldConfig DefaultConfiguration => new();

        protected override ValidationResult Validate(StoredFileReference value, FileFieldConfig configuration)
        {
            return ValidationResult.Success();
        }
    }
}
