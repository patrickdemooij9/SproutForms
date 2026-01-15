using SproutForms.Core.Builders;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    public class TestFileFormCode : ICodeFirstForm
    {
        public string Alias => "testFileForm";

        public FormDefinition Build()
        {
            return new FormBuilder("testFileForm", "TestFileForm")
                .Row(row => row
                    .Col(6, col => col
                        .Text("firstName", "First name")
                        .Set(c => c.Placeholder = "Enter your first name here")
                        .Required()
                        .Done()
                    )
                    .Col(6, col => col
                        .Text("lastName", "Last name")
                        .Set(c => c.Placeholder = "Enter your last name here")
                        .Required()
                        .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .File("file", "Test file")
                        .Set(c => c.StorageProviderAlias = "local")
                        .Set(c => c.AllowedExtensions = [".png"])
                        .Done()
                    )
                ).Build();
        }
    }
}
