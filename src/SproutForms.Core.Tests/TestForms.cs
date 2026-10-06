using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SproutForms.Core.Builders;
using SproutForms.Core.Fields;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using System.Text.Json;

namespace SproutForms.Core.Tests
{
    internal static class TestForms
    {
        public static readonly IFormFieldType[] FieldTypes =
        [
            new TextFieldFormFieldType(),
            new EmailFieldType(),
            new TextAreaFieldType(),
            new CheckboxFieldType(),
            new SelectFieldType(),
            new HiddenFieldType(),
            new RadioFieldType(),
            new DateFieldType(),
            new FileFieldType(),
            new RepeaterFieldType()
        ];

        // A name, and a repeater of people with a required first name, an allergies checkbox and the allergies it asks for, and a CV
        public static FormVersion People(Action<Fields.Configs.RepeaterFieldConfig>? configure = null)
        {
            var definition = new FormBuilder("people", "People")
                .Row(row => row.Col(12, column => column.Text("name", "Name").Done()))
                .Row(row => row.Col(12, column => column
                    .Repeater("people", "People", group => group
                        .Row(entry => entry
                            .Col(6, child => child.Text("firstName", "First name").Required().Done())
                            .Col(6, child => child.Checkbox("hasAllergies", "Has allergies").Done()))
                        .Row(entry => entry
                            .Col(6, child => child.Text("allergies", "Allergies").Done())
                            .Col(6, child => child.File("cv", "CV").Done())))
                    .Set(config => configure?.Invoke(config))
                    .Done()))
                .Build();

            // Only asked for when this entry says it has allergies
            definition.FindField("allergies")!.Rules =
            [
                new FieldRule { Condition = When("hasAllergies", "true"), Action = FieldRuleAction.Show },
                new FieldRule { Condition = When("hasAllergies", "true"), Action = FieldRuleAction.Require }
            ];

            return new FormVersion
            {
                Id = Guid.NewGuid(),
                FormId = Guid.NewGuid(),
                Definition = definition,
                DefinitionHash = "test",
                CreatedBy = "test"
            };
        }

        public static ConditionDefinition When(string fieldAlias, string value) => new()
        {
            Rules = [new ConditionRule { FieldAlias = fieldAlias, Comparison = ConditionComparison.Equals, Value = value }]
        };

        public static FormSubmissionService SubmissionService(IFormSubmissionRepository? submissions = null, params IFormFileStorageProvider[] storageProviders)
        {
            var options = Substitute.For<IOptionsMonitor<SproutFormsOptions>>();
            options.CurrentValue.Returns(new SproutFormsOptions());

            return new FormSubmissionService(
                submissions ?? Substitute.For<IFormSubmissionRepository>(),
                FieldTypes,
                [new StandardFormDefinitionType()],
                new ConditionEvaluator(),
                new FormCalculator(new ConditionEvaluator()),
                Substitute.For<IWorkflowExecutionRepository>(),
                storageProviders,
                Substitute.For<IUnitOfWorkProvider>(),
                Substitute.For<IHttpContextAccessor>(),
                options,
                NullLogger<FormSubmissionService>.Instance);
        }

        public static Dictionary<string, JsonElement> Values(object values)
            => JsonSerializer.SerializeToElement(values).EnumerateObject().ToDictionary(it => it.Name, it => it.Value);
    }
}
