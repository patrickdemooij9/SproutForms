using Microsoft.AspNetCore.Http;
using NSubstitute;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Core.Tests
{
    public class RepeaterSubmissionTests
    {
        [Test]
        public async Task Entries_left_empty_are_dropped_before_saving()
        {
            var submissions = Substitute.For<IFormSubmissionRepository>();
            var service = TestForms.SubmissionService(submissions);

            var result = await Submit(service, TestForms.People(), new
            {
                people = new[]
                {
                    new { firstName = "", hasAllergies = "false" },
                    new { firstName = "Ann", hasAllergies = "false" },
                    new { firstName = " ", hasAllergies = "false" }
                }
            });

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            Assert.That(result.Submission!.Values["people"].GetRawText(), Is.EqualTo("""[{"firstName":"Ann","hasAllergies":"false"}]"""));
            submissions.Received(1).Add(result.Submission);
        }

        [Test]
        public async Task An_entry_error_uses_the_index_it_was_posted_with()
        {
            var result = await Submit(TestForms.SubmissionService(), TestForms.People(), new
            {
                people = new[]
                {
                    new { firstName = "", hasAllergies = "false" },
                    new { firstName = "", hasAllergies = "true" }
                }
            });

            Assert.That(result.Errors.Keys, Is.EquivalentTo(new[] { "people[1].firstName", "people[1].allergies" }));
        }

        [Test]
        public async Task A_condition_uses_the_field_of_its_own_entry()
        {
            var result = await Submit(TestForms.SubmissionService(), TestForms.People(), new
            {
                people = new[]
                {
                    new { firstName = "Ann", hasAllergies = "false" },
                    new { firstName = "Bob", hasAllergies = "true" }
                }
            });

            Assert.That(result.Errors.Keys, Is.EquivalentTo(new[] { "people[1].allergies" }));
        }

        [Test]
        public async Task A_condition_in_an_entry_can_use_a_field_of_the_form()
        {
            var version = TestForms.People();
            version.Definition.FindField("allergies")!.Conditions!.Required!.Rules[0] = new Models.Conditions.ConditionRule
            {
                FieldAlias = "name",
                Comparison = Models.Conditions.ConditionComparison.Equals,
                Value = "Camp"
            };

            var result = await Submit(TestForms.SubmissionService(), version, new
            {
                name = "Camp",
                people = new[] { new { firstName = "Ann", hasAllergies = "true" } }
            });

            Assert.That(result.Errors.Keys, Is.EquivalentTo(new[] { "people[0].allergies" }));
        }

        [TestCase(0, "Add at least 1 entry.")]
        [TestCase(3, "Add no more than 2 entries.")]
        public async Task The_number_of_filled_in_entries_is_checked(int count, string error)
        {
            var version = TestForms.People(config =>
            {
                config.MinItems = 1;
                config.MaxItems = 2;
            });
            var people = Enumerable.Range(0, count).Select(index => new { firstName = $"Person {index}", hasAllergies = "false" })
                .Append(new { firstName = "", hasAllergies = "false" });

            var result = await Submit(TestForms.SubmissionService(), version, new { people });

            Assert.That(result.Errors["people"], Is.EqualTo(new[] { error }));
        }

        [Test]
        public async Task A_required_repeater_needs_a_filled_in_entry()
        {
            var version = TestForms.People();
            version.Definition.FindField("people")!.Required = true;

            var result = await Submit(TestForms.SubmissionService(), version, new
            {
                people = new[] { new { firstName = "", hasAllergies = "false" } }
            });

            Assert.That(result.Errors["people"], Is.EqualTo(new[] { FormTexts.Required }));
        }

        [Test]
        public async Task An_upload_in_an_entry_is_stored_in_that_entry()
        {
            var reference = new StoredFileReference(Guid.NewGuid(), "cv.pdf", 3, "application/pdf", "default");
            var storage = Substitute.For<IFormFileStorageProvider>();
            storage.Alias.Returns("default");
            storage.SaveAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>()).Returns(reference);
            var file = Substitute.For<IFormFile>();
            file.Name.Returns("people[1].cv");
            file.FileName.Returns("cv.pdf");
            file.Length.Returns(3);

            var result = await Submit(TestForms.SubmissionService(null, storage), TestForms.People(), new
            {
                people = new[]
                {
                    new { firstName = "Ann", hasAllergies = "false" },
                    new { firstName = "Bob", hasAllergies = "false" }
                }
            }, file);

            Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Keys));
            var entries = result.Submission!.Values["people"].EnumerateArray().ToList();
            Assert.That(entries[0].TryGetProperty("cv", out _), Is.False);
            Assert.That(JsonSerializer.Deserialize<StoredFileReference>(entries[1].GetProperty("cv").GetString()!), Is.EqualTo(reference));
        }

        [Test]
        public async Task A_rejected_upload_keeps_its_entry_and_its_error()
        {
            var version = TestForms.People();
            ((Fields.Configs.FileFieldConfig)version.Definition.FindField("cv")!.Configuration).AllowedExtensions = [".pdf"];
            var file = Substitute.For<IFormFile>();
            file.Name.Returns("people[0].cv");
            file.FileName.Returns("cv.exe");

            var result = await Submit(TestForms.SubmissionService(), version, new
            {
                people = new[] { new { firstName = "", hasAllergies = "false" } }
            }, file);

            Assert.That(result.Errors.Keys, Is.EquivalentTo(new[] { "people[0].cv", "people[0].firstName" }));
        }

        [Test]
        public void Validating_a_page_checks_the_entries_without_their_uploads()
        {
            var errors = TestForms.SubmissionService().ValidatePage(TestForms.People(), 0, TestForms.Values(new
            {
                people = new[] { new { firstName = "", hasAllergies = "true", allergies = "Nuts" } }
            }));

            Assert.That(errors.Keys, Is.EquivalentTo(new[] { "people[0].firstName" }));
        }

        private static Task<FormSubmissionResult> Submit(Services.FormSubmissionService service, FormVersion version, object values, params IFormFile[] files)
            => service.SubmitAsync(version, new FormSubmissionRequest { Values = TestForms.Values(values) }, files);
    }
}
