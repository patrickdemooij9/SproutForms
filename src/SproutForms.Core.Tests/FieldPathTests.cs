using SproutForms.Core.Helpers;
using System.Text.Json;

namespace SproutForms.Core.Tests
{
    public class FieldPathTests
    {
        [TestCase("name", "name")]
        [TestCase("people[0].firstName", "people[0].firstName")]
        [TestCase("people[12].kids[3].name", "people[12].kids[3].name")]
        public void Parses_a_field_or_an_entry_path(string value, string expected)
        {
            Assert.That(FieldPath.TryParse(value, out var path), Is.True);
            Assert.That(path!.ToString(), Is.EqualTo(expected));
        }

        [TestCase("people[0]")]
        [TestCase("people.firstName")]
        [TestCase("people[x].firstName")]
        [TestCase("people[1000].firstName")]
        [TestCase("people[0].")]
        [TestCase("")]
        public void Rejects_what_isnt_a_path(string value)
        {
            Assert.That(FieldPath.TryParse(value, out _), Is.False);
        }

        [Test]
        public void Setting_a_value_adds_the_entries_before_it()
        {
            var values = new Dictionary<string, JsonElement>();
            FieldPath.TryParse("people[2].firstName", out var path);

            path!.SetValue(values, JsonSerializer.SerializeToElement("Ann"));

            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{},{},{"firstName":"Ann"}]"""));
        }

        [Test]
        public void Setting_a_value_keeps_the_entry_other_values()
        {
            var values = TestForms.Values(new { people = new[] { new { firstName = "Ann" } } });
            FieldPath.TryParse("people[0].allergies", out var path);

            path!.SetValue(values, JsonSerializer.SerializeToElement("Nuts"));

            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann","allergies":"Nuts"}]"""));
        }
    }
}
