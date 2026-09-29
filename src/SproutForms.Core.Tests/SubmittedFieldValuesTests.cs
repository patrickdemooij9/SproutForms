using SproutForms.Core.Helpers;
using System.Text.Json;

namespace SproutForms.Core.Tests
{
    public class SubmittedFieldValuesTests
    {
        [Test]
        public void A_posted_form_puts_each_entry_input_in_its_entry()
        {
            var values = SubmittedFieldValues.FromPostedForm(new Dictionary<string, string>
            {
                ["name"] = "Team",
                ["people[0].firstName"] = "Ann",
                ["people[1].firstName"] = "Bob",
                ["people[1].hasAllergies"] = "true"
            });

            Assert.That(values["name"].GetString(), Is.EqualTo("Team"));
            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann"},{"firstName":"Bob","hasAllergies":"true"}]"""));
        }

        [Test]
        public void Filtering_keeps_only_the_group_own_fields_and_never_a_posted_file_value()
        {
            var version = TestForms.People();
            var values = SubmittedFieldValues.Filter(version, TestForms.Values(new
            {
                name = "Team",
                unknown = "x",
                people = new object[]
                {
                    new { firstName = "Ann", cv = "forged", name = "not in the entry" },
                    "not an entry"
                }
            }));

            Assert.That(values.Keys, Is.EquivalentTo(new[] { "name", "people" }));
            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann"},{}]"""));
        }

        [Test]
        public void Filtering_drops_a_group_value_that_isnt_a_list()
        {
            var values = SubmittedFieldValues.Filter(TestForms.People(), TestForms.Values(new { people = "Ann" }));

            Assert.That(values, Does.Not.ContainKey("people"));
        }

        [Test]
        public void Json_numbers_and_booleans_become_text_inside_entries_too()
        {
            var values = SubmittedFieldValues.FromJson(TestForms.Values(new
            {
                age = 3,
                people = new[] { new { hasAllergies = true, count = 2 } }
            })).ToDictionary(it => it.Key, it => it.Value);

            Assert.That(values["age"].GetString(), Is.EqualTo("3"));
            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{"hasAllergies":"true","count":"2"}]"""));
        }
    }
}
