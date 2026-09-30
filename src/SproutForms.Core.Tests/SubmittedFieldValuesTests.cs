using SproutForms.Core.Helpers;

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
        public void A_posted_value_named_after_a_group_makes_way_for_its_entries()
        {
            var values = SubmittedFieldValues.FromPostedForm(new Dictionary<string, string>
            {
                ["people"] = "x",
                ["people[0].firstName"] = "Ann"
            });

            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann"}]"""));
        }

        [Test]
        public void Only_the_group_own_fields_are_taken_and_never_a_posted_file_value()
        {
            var values = Parse(new
            {
                name = "Team",
                unknown = "x",
                people = new object[]
                {
                    new { firstName = "Ann", cv = "forged", name = "not in the entry" },
                    "not an entry"
                }
            });

            Assert.That(values.Keys, Is.EquivalentTo(new[] { "name", "people" }));
            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann"},{}]"""));
        }

        [Test]
        public void A_group_value_that_isnt_a_list_is_dropped()
        {
            Assert.That(Parse(new { people = "Ann" }), Does.Not.ContainKey("people"));
        }

        [Test]
        public void Json_numbers_and_booleans_become_text_inside_entries_too()
        {
            var values = Parse(new
            {
                name = 3,
                people = new[] { new { hasAllergies = (object)true, firstName = (object)2 } }
            });

            Assert.That(values["name"].GetString(), Is.EqualTo("3"));
            Assert.That(values["people"].GetRawText(), Is.EqualTo("""[{"hasAllergies":"true","firstName":"2"}]"""));
        }

        private static Dictionary<string, System.Text.Json.JsonElement> Parse(object values)
            => SubmittedValues.Parse(TestForms.People().Definition.Fields, TestForms.Values(values)).ToDictionary();
    }
}
