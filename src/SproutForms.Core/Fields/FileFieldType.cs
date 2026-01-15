using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;

namespace SproutForms.Core.Fields
{
    public class FileFieldType : FormFieldBase<FileFieldConfig, StoredFileReference>
    {
        public override Guid Id => Guid.Parse("382ab784-4b24-4052-a7b4-429442c67eca");

        public override string Alias => "file";

        public override string DisplayName => "File";

        public override FileFieldConfig DefaultConfiguration => new();

        protected override ValidationResult Validate(StoredFileReference value, FileFieldConfig configuration)
        {
            return ValidationResult.Success();
        }
    }
}
