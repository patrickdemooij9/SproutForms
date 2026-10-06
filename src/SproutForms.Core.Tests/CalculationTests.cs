using Microsoft.Extensions.Logging.Abstractions;
using SproutForms.Core.Builders;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Services;

namespace SproutForms.Core.Tests
{
    public class CalculationTests
    {
        private static readonly FormCalculator Calculator = new(new ConditionEvaluator());

        // Two questions worth 10 and 5 points, and a score for the answers
        private static FormBuilder Quiz() => new FormBuilder("quiz", "Quiz")
            .Row(row => row.Col(12, column => column.Text("capital", "Capital of France").Done()))
            .Row(row => row.Col(12, column => column.Text("planets", "Planets").Done()))
            .Variable("score")
            .Calculate("score", rules => rules
                .When(c => c.Field("capital").Is("Paris")).Add(10)
                .When(c => c.Field("planets").Is("8")).Add(5));

        [Test]
        public void Rules_run_top_to_bottom_on_the_answers()
        {
            var variables = Calculator.Calculate(Quiz().Build(), TestForms.Values(new { capital = "paris", planets = "9" }));

            Assert.That(variables["score"].GetRawText(), Is.EqualTo("10"));
        }

        [Test]
        public void A_hidden_field_counts_as_empty()
        {
            var definition = new FormBuilder("quiz", "Quiz")
                .Row(row => row.Col(12, column => column.Text("wantsBonus", "Bonus question?").Done()))
                .Row(row => row.Col(12, column => column.Text("bonus", "Bonus").VisibleWhen(c => c.Field("wantsBonus").Is("yes")).Done()))
                .Variable("score")
                .Calculate("score", rules => rules.When(c => c.Field("bonus").Is("42")).Add(1))
                .Build();

            var hidden = Calculator.Calculate(definition, TestForms.Values(new { wantsBonus = "no", bonus = "42" }));
            var shown = Calculator.Calculate(definition, TestForms.Values(new { wantsBonus = "yes", bonus = "42" }));

            Assert.That(hidden["score"].GetRawText(), Is.EqualTo("0"));
            Assert.That(shown["score"].GetRawText(), Is.EqualTo("1"));
        }

        [Test]
        public void The_highest_counter_wins_by_comparing_variables()
        {
            var definition = new FormBuilder("personality", "Personality")
                .Row(row => row.Col(12, column => column.Text("party", "At a party you…").Done()))
                .Row(row => row.Col(12, column => column.Text("weekend", "Your weekend").Done()))
                .Variable("introvert")
                .Variable("extrovert")
                .Variable("personality", v => v.Text().StartAt("Ambivert"))
                .Calculate("introvert", rules => rules
                    .When(c => c.Field("party").Is("corner")).Add(1)
                    .When(c => c.Field("weekend").Is("book")).Add(1))
                .Calculate("extrovert", rules => rules
                    .When(c => c.Field("party").Is("dance")).Add(1))
                .Calculate("personality", rules => rules
                    .When(c => c.Variable("introvert").GreaterThan(ValueOf.Variable("extrovert"))).Set("Introvert")
                    .When(c => c.Variable("extrovert").GreaterThan(ValueOf.Variable("introvert"))).Set("Extrovert"))
                .Build();

            var variables = Calculator.Calculate(definition, TestForms.Values(new { party = "dance", weekend = "book" }));
            Assert.That(variables["personality"].GetString(), Is.EqualTo("Ambivert"));

            variables = Calculator.Calculate(definition, TestForms.Values(new { party = "corner", weekend = "book" }));
            Assert.That(variables["personality"].GetString(), Is.EqualTo("Introvert"));
        }

        [Test]
        public void Numbers_use_field_values_and_round_to_the_variables_decimals()
        {
            var definition = new FormBuilder("quote", "Quote")
                .Row(row => row.Col(12, column => column.Text("quantity", "Quantity").Done()))
                .Variable("price", v => v.Number(decimals: 2).StartAt(4.995m))
                .Variable("perPerson", v => v.Number(decimals: 2))
                .Calculate("price", rules => rules.Always().Multiply(ValueOf.Field("quantity")))
                .Calculate("perPerson", rules => rules
                    .Always().Set(ValueOf.Variable("price"))
                    .Always().Divide(0)
                    .Always().Divide(3))
                .Build();

            var variables = Calculator.Calculate(definition, TestForms.Values(new { quantity = "3 pieces" }));

            Assert.That(variables["price"].GetRawText(), Is.EqualTo("14.99"));
            Assert.That(variables["perPerson"].GetRawText(), Is.EqualTo("5"));
        }

        [Test]
        public void Text_that_isnt_a_number_counts_as_zero()
        {
            var definition = new FormBuilder("quote", "Quote")
                .Row(row => row.Col(12, column => column.Text("quantity", "Quantity").Done()))
                .Variable("total", v => v.StartAt(1))
                .Calculate("total", rules => rules.Always().Add(ValueOf.Field("quantity")))
                .Build();

            var variables = Calculator.Calculate(definition, TestForms.Values(new { quantity = "many" }));

            Assert.That(variables["total"].GetRawText(), Is.EqualTo("1"));
        }

        [Test]
        public async Task A_page_skipped_because_of_a_variable_isnt_validated()
        {
            var definition = new FormBuilder("quiz", "Quiz")
                .Page("Question", page => page.Row(row => row.Col(12, column => column.Text("capital", "Capital").Done())))
                .Page("Retry", page => page
                    .VisibleWhen(c => c.Variable("score").LessThan(10))
                    .Row(row => row.Col(12, column => column.Text("why", "Why?").Required().Done())))
                .Variable("score")
                .Calculate("score", rules => rules.When(c => c.Field("capital").Is("Paris")).Add(10))
                .Build();
            Assert.That(FormDefinitionStructureValidator.Validate(definition), Is.Empty);

            var service = TestForms.SubmissionService();
            var passed = await service.SubmitAsync(Version(definition), new FormSubmissionRequest { Values = TestForms.Values(new { capital = "Paris" }) }, []);
            var failed = await service.SubmitAsync(Version(definition), new FormSubmissionRequest { Values = TestForms.Values(new { capital = "Rome" }) }, []);

            Assert.That(passed.IsValid, Is.True, string.Join(", ", passed.Errors.Keys));
            Assert.That(passed.Submission!.Variables["score"].GetRawText(), Is.EqualTo("10"));
            Assert.That(failed.Errors.Keys, Is.EquivalentTo(new[] { "why" }));
        }

        [Test]
        public void A_visibility_condition_cant_use_a_variable_that_depends_on_the_field_itself()
        {
            var definition = new FormBuilder("loop", "Loop")
                .Row(row => row.Col(12, column => column.Text("answer", "Answer").VisibleWhen(c => c.Variable("score").Is(0)).Done()))
                .Variable("score")
                .Calculate("score", rules => rules.When(c => c.Field("answer").IsNotEmpty()).Add(1))
                .Build();

            Assert.That(FormDefinitionStructureValidator.Validate(definition), Has.Some.Contains("depends on field 'Answer' itself"));
        }

        [Test]
        public void A_page_condition_cant_use_a_variable_that_depends_on_a_later_page()
        {
            var definition = new FormBuilder("later", "Later")
                .Page("One", page => page.Row(row => row.Col(12, column => column.Text("first", "First").Done())))
                .Page("Two", page => page
                    .VisibleWhen(c => c.Variable("score").GreaterThan(0))
                    .Row(row => row.Col(12, column => column.Text("second", "Second").Done())))
                .Variable("score")
                .Calculate("score", rules => rules.When(c => c.Field("second").IsNotEmpty()).Add(1))
                .Build();

            Assert.That(FormDefinitionStructureValidator.Validate(definition), Has.Some.Contains("depends on field 'Second', which isn't on an earlier page"));
        }

        [Test]
        public void Rules_must_use_variables_the_form_has_and_fit_their_type()
        {
            var definition = Quiz()
                .Variable("name", v => v.Text())
                .Calculate("name", rules => rules.Always().Multiply(2))
                .Calculate("missing", rules => rules.Always().Add(1))
                .Calculate("score", rules => rules.When(c => c.Variable("unknown").Is(1)).Add(1))
                .Build();

            var errors = FormDefinitionStructureValidator.Validate(definition);

            Assert.That(errors, Has.Some.Contains("can't multiply variable 'name'"));
            Assert.That(errors, Has.Some.Contains("changes variable 'missing'"));
            Assert.That(errors, Has.Some.Contains("uses variable 'unknown'"));
        }

        [Test]
        public async Task The_first_conditional_outcome_that_holds_is_used_with_its_tokens_filled_in()
        {
            var definition = Quiz()
                .Variable("verdict", v => v.Text())
                .Calculate("verdict", rules => rules.Always().Set("<b>").Always().Append(ValueOf.Field("capital")).Always().Append("</b>"))
                .SetOutcome(ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "Try again" })
                .SetOutcomeWhen(c => c.Variable("score").GreaterThan(12), ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "Perfect: {var:score}" })
                .SetOutcomeWhen(c => c.Variable("score").GreaterThan(5), ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "{var:score} points for {var:verdict}" })
                .Build();
            var version = Version(definition);
            var runner = new FormSubmitOutcomeRunner([new ShowMessageOutcome()], new ConditionEvaluator(), NullLogger<FormSubmitOutcomeRunner>.Instance);

            var values = TestForms.Values(new { capital = "Paris", planets = "7" });
            var submission = new FormSubmission
            {
                Values = values,
                Variables = Calculator.Calculate(definition, values)
            };

            var result = await runner.RunAsync(version, submission, CancellationToken.None);

            Assert.That(result!.Data["message"], Is.EqualTo("10 points for &lt;b&gt;Paris&lt;/b&gt;"));
        }

        [Test]
        public async Task Tokens_are_encoded_for_redirect_urls_and_formatted_with_their_decimals_in_workflows()
        {
            var definition = new FormBuilder("quote", "Quote")
                .Row(row => row.Col(12, column => column.Text("city", "City").Done()))
                .Variable("total", v => v.Number(decimals: 2).StartAt(12.5m))
                .Variable("city", v => v.Text())
                .Calculate("city", rules => rules.Always().Set(ValueOf.Field("city")))
                .SetOutcome(RedirectUrlOutcomeType.Alias, new RedirectUrlOutcomeConfig { RedirectUrl = "/thanks?total={var:total}&city={var:city}" })
                .Build();
            var values = TestForms.Values(new { city = "Den Haag & co" });
            var submission = new FormSubmission { Values = values, Variables = Calculator.Calculate(definition, values) };
            var runner = new FormSubmitOutcomeRunner([new RedirectUrlOutcomeType()], new ConditionEvaluator(), NullLogger<FormSubmitOutcomeRunner>.Instance);

            var redirect = await runner.RunAsync(Version(definition), submission, CancellationToken.None);
            var message = new WorkflowMessageResolver(new FormValueFormatter(TestForms.FieldTypes))
                .ResolveTokens("Total {var:total} for {city}, unknown '{var:missing}'", submission, Version(definition));

            Assert.That(redirect!.Data["url"], Is.EqualTo("/thanks?total=12.50&city=Den%20Haag%20%26%20co"));
            Assert.That(message, Is.EqualTo("Total 12.50 for Den Haag & co, unknown ''"));
        }

        [Test]
        public void The_browser_only_gets_the_variables_its_conditions_need()
        {
            var definition = new FormBuilder("mixed", "Mixed")
                .Page("One", page => page.Row(row => row.Col(12, column => column.Text("capital", "Capital").Done())))
                .Page("Two", page => page
                    .VisibleWhen(c => c.Variable("level").Is("hard"))
                    .Row(row => row.Col(12, column => column.Text("extra", "Extra").Done())))
                .Variable("score")
                .Variable("level", v => v.Text())
                .Variable("base")
                .Variable("price")
                .Calculate("base", rules => rules.Always().Set(5))
                .Calculate("level", rules => rules.When(c => c.Variable("base").GreaterThan(1)).Set("hard"))
                .Calculate("score", rules => rules.When(c => c.Field("capital").Is("Paris")).Add(10))
                .Calculate("price", rules => rules.Always().Set(20))
                .Build();

            var client = new FormClientModelBuilder(TestForms.FieldTypes, [new StandardFormDefinitionType()])
                .Build(new Form { Id = Guid.NewGuid(), Alias = "mixed", Name = "Mixed" }, Version(definition));

            Assert.That(client.Variables.Select(it => it.Alias), Is.EquivalentTo(new[] { "level", "base" }));
            Assert.That(client.Calculations.Select(it => it.VariableAlias), Is.EqualTo(new[] { "base", "level" }));
        }

        [TestCase("no", "", false, false)]
        [TestCase("yes", "", true, false)]
        [TestCase("yes", "secret", false, false)]
        [TestCase("maybe", "", true, true)]
        public void Field_rules_show_hide_and_require(string answer, string hideWhen, bool visible, bool required)
        {
            var definition = new FormBuilder("rules", "Rules")
                .Row(row => row.Col(12, column => column.Text("answer", "Answer").Done()))
                .Row(row => row.Col(12, column => column.Text("code", "Code").Done()))
                .Row(row => row.Col(12, column => column.Text("details", "Details")
                    .VisibleWhen(c => c.Field("answer").Is("yes"))
                    .VisibleWhen(c => c.Field("answer").Is("maybe"))
                    .HiddenWhen(c => c.Field("code").Is("secret"))
                    .RequiredWhen(c => c.Field("answer").Is("maybe"))
                    .Done()))
                .Build();
            var evaluator = new ConditionEvaluator();
            var values = TestForms.Values(new { answer, code = hideWhen });
            var details = definition.FindField("details")!;

            Assert.That(evaluator.IsVisible(details, values), Is.EqualTo(visible));
            Assert.That(evaluator.IsRequired(details, values), Is.EqualTo(required));
        }

        private static FormVersion Version(FormDefinition definition) => new()
        {
            Id = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Definition = definition,
            DefinitionHash = "test",
            CreatedBy = "test"
        };
    }
}
