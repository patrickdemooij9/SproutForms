using Microsoft.AspNetCore.Http;
using NSubstitute;
using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Files;
using System.Text.Json;

namespace SproutForms.Core.Tests
{
    public class HiddenFieldSubmissionTests
    {
        [Test]
        public async Task A_field_hidden_by_its_rules_is_neither_validated_nor_stored()
        {
            var result = await Submit(TestForms.SubmissionService(), Contact(), new
            {
                contactBy = "phone",
                email = "not an email",
                phone = "0123"
            });

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values.Keys, Is.EquivalentTo(new[] { "contactBy", "phone", "source" }));
        }

        [Test]
        public async Task The_fields_of_a_skipped_page_are_not_stored()
        {
            var version = Version(new FormBuilder("delivery", "Delivery")
                .Page("You", page => page.Row(row => row.Col(12, column => column.Text("delivery", "Delivery").Done())))
                .Page("Address", page => page
                    .VisibleWhen(condition => condition.Field("delivery", ConditionComparison.Equals, "home"))
                    .Row(row => row.Col(12, column => column.Text("address", "Address").Required().Done())))
                .Build());

            var result = await Submit(TestForms.SubmissionService(), version, new { delivery = "pickup", address = "Somewhere 1" });

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values.Keys, Is.EquivalentTo(new[] { "delivery" }));
        }

        [Test]
        public async Task A_rejected_submission_gives_back_the_values_of_hidden_fields()
        {
            var result = await Submit(TestForms.SubmissionService(), Contact(), new
            {
                contactBy = "phone",
                email = "ann@example.com"
            });

            Assert.That(result.Errors.Keys, Is.EquivalentTo(new[] { "phone" }));
            Assert.That(result.Values["email"].GetString(), Is.EqualTo("ann@example.com"));
        }

        [Test]
        public async Task A_field_hidden_in_an_entry_is_not_stored_in_that_entry()
        {
            var result = await Submit(TestForms.SubmissionService(), TestForms.People(), new
            {
                people = new[] { new { firstName = "Ann", hasAllergies = "false", allergies = "Nuts" } }
            });

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann","hasAllergies":"false"}]"""));
        }

        [TestCase("print")]
        [TestCase("")]
        public async Task A_locked_hidden_field_stores_its_own_value_whatever_was_posted(string posted)
        {
            var result = await Submit(TestForms.SubmissionService(), Contact(), new { contactBy = "phone", phone = "0123", source = posted });

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values["source"].GetString(), Is.EqualTo("web"));
        }

        [Test]
        public async Task A_locked_hidden_field_stores_its_own_value_when_nothing_was_posted()
        {
            var result = await Submit(TestForms.SubmissionService(), Contact(), new { contactBy = "phone", phone = "0123" });

            Assert.That(result.Submission!.Values["source"].GetString(), Is.EqualTo("web"));
        }

        [Test]
        public async Task A_locked_hidden_field_without_a_value_stores_nothing()
        {
            var version = Contact();
            ((HiddenFieldConfig)version.Definition.FindField("source")!.Configuration).DefaultValue = null;

            var result = await Submit(TestForms.SubmissionService(), version, new { contactBy = "phone", phone = "0123", source = "print" });

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values.ContainsKey("source"), Is.False);
        }

        [Test]
        public async Task A_hidden_field_that_may_be_changed_stores_what_was_posted()
        {
            var version = Contact();
            ((HiddenFieldConfig)version.Definition.FindField("source")!.Configuration).AllowOverrideFromClient = true;

            var result = await Submit(TestForms.SubmissionService(), version, new { contactBy = "phone", phone = "0123", source = "print" });

            Assert.That(result.Submission!.Values["source"].GetString(), Is.EqualTo("print"));
        }

        [Test]
        public async Task Conditions_see_the_value_of_a_locked_hidden_field_not_the_posted_one()
        {
            var version = Contact();
            version.Definition.FindField("phone")!.Rules.Add(new FieldRule
            {
                Condition = TestForms.When("source", "web"),
                Action = FieldRuleAction.Require
            });

            var result = await Submit(TestForms.SubmissionService(), version, new { contactBy = "phone", source = "print" });

            Assert.That(result.Errors.Keys, Is.EquivalentTo(new[] { "phone" }));
        }

        [Test]
        public async Task An_entry_with_only_a_locked_hidden_value_is_left_empty()
        {
            var version = TestForms.People();
            var group = (RepeaterFieldConfig)version.Definition.FindField("people")!.Configuration;
            group.Fields.Add(new FormField
            {
                Alias = "kind",
                Label = "Kind",
                FieldTypeAlias = "hidden",
                Configuration = new HiddenFieldConfig { DefaultValue = "guest" }
            });

            var result = await Submit(TestForms.SubmissionService(), version, new
            {
                people = new object[]
                {
                    new { firstName = "Ann", hasAllergies = "false" },
                    new { firstName = "", hasAllergies = "false", kind = "guest" }
                }
            });

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann","hasAllergies":"false","kind":"guest"}]"""));
        }

        [Test]
        public async Task An_upload_for_a_hidden_field_is_deleted_and_not_stored()
        {
            var version = Contact();
            version.Definition.Fields.Add(new FormField
            {
                Alias = "cv",
                Label = "CV",
                FieldTypeAlias = "file",
                Configuration = new FileFieldConfig(),
                Rules = [new FieldRule { Condition = TestForms.When("contactBy", "email"), Action = FieldRuleAction.Show }]
            });
            var storage = Storage();

            var result = await Submit(TestForms.SubmissionService(null, storage), version, new { contactBy = "phone", phone = "0123" }, Upload("cv"));

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values.ContainsKey("cv"), Is.False);
            await storage.Received(1).DeleteAsync(Arg.Any<StoredFileReference>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Only_the_last_of_several_uploads_for_one_field_is_kept()
        {
            var storage = Storage();

            var result = await Submit(TestForms.SubmissionService(null, storage), TestForms.People(), new
            {
                people = new[] { new { firstName = "Ann", hasAllergies = "false" } }
            }, Upload("people[0].cv"), Upload("people[0].cv"), Upload("people[0].cv"));

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            var kept = JsonSerializer.Deserialize<StoredFileReference>(result.Submission!.Values["people"][0].GetProperty("cv").GetString()!);
            await storage.Received(2).DeleteAsync(Arg.Is<StoredFileReference>(reference => reference != kept), Arg.Any<CancellationToken>());
            await storage.DidNotReceive().DeleteAsync(kept!, Arg.Any<CancellationToken>());
        }

        // How to reach the visitor: an email address or a phone number, each shown only for its choice, and a locked hidden "source"
        private static FormVersion Contact()
            => Version(new FormBuilder("contact", "Contact")
                .Row(row => row.Col(12, column => column.Text("contactBy", "Contact by").Done()))
                .Row(row => row.Col(12, column => column.Email("email", "Email")
                    .Required()
                    .VisibleWhen(condition => condition.Field("contactBy", ConditionComparison.Equals, "email"))
                    .Done()))
                .Row(row => row.Col(12, column => column.Text("phone", "Phone")
                    .Required()
                    .VisibleWhen(condition => condition.Field("contactBy", ConditionComparison.Equals, "phone"))
                    .Done()))
                .Row(row => row.Col(12, column => column.Hidden("source", "Source")
                    .Set(config => config.DefaultValue = "web")
                    .Done()))
                .Build());

        private static FormVersion Version(FormDefinition definition) => new()
        {
            Id = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Definition = definition,
            DefinitionHash = "test",
            CreatedBy = "test"
        };

        // Every upload gets a reference of its own
        private static IFormFileStorageProvider Storage()
        {
            var storage = Substitute.For<IFormFileStorageProvider>();
            storage.Alias.Returns("default");
            storage.SaveAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>())
                .Returns(_ => new StoredFileReference(Guid.NewGuid(), "cv.pdf", 3, "application/pdf", "default"));
            return storage;
        }

        private static IFormFile Upload(string name)
        {
            var file = Substitute.For<IFormFile>();
            file.Name.Returns(name);
            file.FileName.Returns("cv.pdf");
            file.Length.Returns(3);
            return file;
        }

        private static Task<FormSubmissionResult> Submit(Services.FormSubmissionService service, FormVersion version, object values, params IFormFile[] files)
            => service.SubmitAsync(version, new FormSubmissionRequest { Values = TestForms.Values(values) }, files);
    }
}
