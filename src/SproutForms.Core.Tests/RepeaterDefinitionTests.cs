using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Services;
using System.Text.Json;

namespace SproutForms.Core.Tests
{
    public class RepeaterDefinitionTests
    {
        [Test]
        public void A_repeater_with_its_own_layout_is_valid()
        {
            Assert.That(FormDefinitionStructureValidator.Validate(TestForms.People().Definition), Is.Empty);
        }

        [Test]
        public void An_alias_is_unique_across_the_whole_form()
        {
            var definition = TestForms.People().Definition;
            definition.FindField("firstName")!.Alias = "name";
            Group(definition).Rows[0].Columns[0].FieldAlias = "name";

            Assert.That(FormDefinitionStructureValidator.Validate(definition), Has.Some.Contains("more than one field with alias 'name'"));
        }

        [Test]
        public void A_field_outside_the_repeater_cant_use_a_field_inside_it()
        {
            var definition = TestForms.People().Definition;
            definition.FindField("name")!.Rules = [new() { Condition = TestForms.When("firstName", "Ann") }];

            Assert.That(FormDefinitionStructureValidator.Validate(definition), Has.Some.Contains("which is inside 'People'"));
        }

        [Test]
        public void A_field_of_the_repeater_must_be_placed_in_it()
        {
            var definition = TestForms.People().Definition;
            Group(definition).Rows.RemoveAt(1);

            Assert.That(FormDefinitionStructureValidator.Validate(definition), Has.Some.Contains("Field 'Allergies' isn't placed in 'People'"));
        }

        [Test]
        public void A_repeater_cant_hold_another_repeater()
        {
            var definition = TestForms.People().Definition;
            Group(definition).Fields.Add(new FormField { Alias = "kids", Label = "Kids", FieldTypeAlias = "repeater", Configuration = new RepeaterFieldConfig() });
            Group(definition).Rows.Add(new FormRow { Columns = [new FormColumn { FieldAlias = "kids", Width = 12 }] });

            Assert.That(FormDefinitionStructureValidator.Validate(definition), Has.Some.Contains("'Kids' can't be inside another field group"));
        }

        [Test]
        public void The_definition_survives_being_stored()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonConverters.FormFieldJsonConverter(TestForms.FieldTypes, []));
            var definition = TestForms.People().Definition;

            var stored = JsonSerializer.Deserialize<FormDefinition>(JsonSerializer.Serialize(definition, options), options)!;

            var group = Group(stored);
            Assert.That(group.Fields.Select(it => it.Alias), Is.EqualTo(new[] { "firstName", "hasAllergies", "allergies", "cv" }));
            Assert.That(group.Rows.SelectMany(row => row.Columns).Select(column => column.FieldAlias), Is.EqualTo(new[] { "firstName", "hasAllergies", "allergies", "cv" }));
            Assert.That(stored.FindField("allergies")!.Rules[0].Condition.Rules[0].FieldAlias, Is.EqualTo("hasAllergies"));
        }

        [Test]
        public void The_value_shows_each_entry_with_its_title()
        {
            var version = TestForms.People(config => config.ItemTitle = "Person {n}");
            var formatter = new FormValueFormatter(TestForms.FieldTypes);
            var value = JsonSerializer.SerializeToElement(new[]
            {
                new Dictionary<string, string> { ["firstName"] = "Ann", ["hasAllergies"] = "true", ["allergies"] = "Nuts" },
                new Dictionary<string, string> { ["firstName"] = "Bob" }
            });

            var text = formatter.Format(version.Definition.FindField("people"), value);

            Assert.That(text, Is.EqualTo("Person 1\nFirst name: Ann\nHas allergies: true\nAllergies: Nuts\n\nPerson 2\nFirst name: Bob"));
        }

        private static RepeaterFieldConfig Group(FormDefinition definition)
            => (RepeaterFieldConfig)definition.FindField("people")!.Configuration;
    }
}
