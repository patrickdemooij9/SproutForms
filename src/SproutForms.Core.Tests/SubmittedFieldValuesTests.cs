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

        [Test]
        public void Entries_past_the_maximum_are_ignored()
        {
            var values = Parse(new
            {
                people = Enumerable.Range(0, SubmittedValues.MaxEntries + 500).Select(index => new { firstName = $"Person {index}" })
            });

            Assert.That(values["people"].GetArrayLength(), Is.EqualTo(SubmittedValues.MaxEntries));
        }

        [Test]
        public void The_maximum_counts_the_entries_of_nested_groups_too()
        {
            var children = new Fields.Configs.RepeaterFieldConfig
            {
                Fields = [new Models.FormField { Alias = "childName", Label = "Name", FieldTypeAlias = "text", Configuration = new Fields.Configs.TextFieldConfig() }]
            };
            var families = new Fields.Configs.RepeaterFieldConfig
            {
                Fields = [new Models.FormField { Alias = "children", Label = "Children", FieldTypeAlias = "repeater", Configuration = children }]
            };
            var fields = new List<Models.FormField>
            {
                new() { Alias = "families", Label = "Families", FieldTypeAlias = "repeater", Configuration = families }
            };

            // 20 families of 100 children: 2,020 entries in all
            var values = SubmittedValues.Parse(fields, TestForms.Values(new
            {
                families = Enumerable.Range(0, 20).Select(_ => new
                {
                    children = Enumerable.Range(0, 100).Select(index => new { childName = $"Child {index}" })
                })
            })).ToDictionary();

            var parsed = values["families"].EnumerateArray().ToList();
            var total = parsed.Count + parsed.Sum(family => family.GetProperty("children").GetArrayLength());
            Assert.That(total, Is.EqualTo(SubmittedValues.MaxEntries));
        }

        private static Dictionary<string, System.Text.Json.JsonElement> Parse(object values)
            => SubmittedValues.Parse(TestForms.People().Definition.Fields, TestForms.Values(values)).ToDictionary();
    }
}
