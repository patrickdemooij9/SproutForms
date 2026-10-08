using SproutForms.Core.Models;
using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using System.Text.Json;

namespace SproutForms.Core.Builders
{
    public class FormBuilder
    {
        private readonly List<FormField> _fields = [];
        private readonly List<FormPage> _pages = [];
        private FormPage? _implicitPage;
        private string? _submitLabel;
        private bool _showProgress = true;
        private readonly List<FormWorkflow> _workflows = [];
        private FormSubmitOutcome? _outcome;
        private FormDefinitionTypeReference? _type;
        private readonly List<FormVariable> _variables = [];
        private readonly List<CalculationRule> _calculations = [];
        private readonly List<ConditionalOutcome> _conditionalOutcomes = [];

        public string Alias { get; }
        public string Name { get; }

        public FormBuilder(string alias, string name)
        {
            Alias = alias;
            Name = name;
        }

        private readonly HashSet<string> _aliases = [];

        // A field goes into the form, or into the field group it was added to; either way its alias is unique across the form
        internal FormField RegisterField(FormField field, List<FormField>? groupFields = null)
        {
            if (!_aliases.Add(field.Alias))
                throw new InvalidOperationException($"Duplicate field alias '{field.Alias}'.");

            (groupFields ?? _fields).Add(field);
            return field;
        }

        /// <summary>
        /// Adds a row to the first page, for forms that don't use <see cref="Page"/>.
        /// </summary>
        public FormBuilder Row(Action<RowBuilder> configure)
        {
            if (_implicitPage == null)
            {
                if (_pages.Count > 0)
                    throw new InvalidOperationException("Rows after a page belong to a page. Add them with Page(title, page => page.Row(...)).");

                _implicitPage = new FormPage();
                _pages.Add(_implicitPage);
            }

            var row = new RowBuilder(this);
            configure(row);
            _implicitPage.Rows.Add(row.Build());
            return this;
        }

        public FormBuilder Page(string? title, Action<PageBuilder> configure)
        {
            var page = new PageBuilder(this, title);
            configure(page);
            _pages.Add(page.Build());
            _implicitPage = null;
            return this;
        }

        public FormBuilder SubmitLabel(string label)
        {
            _submitLabel = label;
            return this;
        }

        /// <summary>
        /// Whether a form with more than one page shows its progress steps above the page. They show by default.
        /// </summary>
        public FormBuilder ShowProgress(bool show = true)
        {
            _showProgress = show;
            return this;
        }

        public FormBuilder SetOutcome(IFormSubmitOutcomeType outcome, object configuration)
            => SetOutcome(outcome.Alias, configuration);

        /// <summary>
        /// Sets what the visitor sees after a successful submit, using any registered outcome type.
        /// </summary>
        public FormBuilder SetOutcome(string outcomeTypeAlias, object configuration)
        {
            _outcome = new FormSubmitOutcome
            {
                OutcomeTypeAlias = outcomeTypeAlias,
                Configuration = configuration
            };
            return this;
        }

        /// <summary>
        /// Uses another outcome instead of the one from <see cref="SetOutcome(string, object)"/> when the condition holds, such as a
        /// result page per score. Conditional outcomes are checked in the order they're added; the first that holds is used.
        /// </summary>
        public FormBuilder SetOutcomeWhen(Action<ConditionBuilder> condition, string outcomeTypeAlias, object configuration)
        {
            _conditionalOutcomes.Add(new ConditionalOutcome
            {
                Condition = ConditionBuilder.Build(condition),
                Outcome = new FormSubmitOutcome
                {
                    OutcomeTypeAlias = outcomeTypeAlias,
                    Configuration = configuration
                }
            });
            return this;
        }

        public FormBuilder SetOutcomeWhen(Action<ConditionBuilder> condition, IFormSubmitOutcomeType outcome, object configuration)
            => SetOutcomeWhen(condition, outcome.Alias, configuration);

        /// <summary>
        /// Adds a variable for the calculations to work out, a number that starts at 0 unless configured otherwise.
        /// </summary>
        public FormBuilder Variable(string alias, Action<VariableBuilder>? configure = null)
        {
            if (_variables.Any(it => it.Alias == alias))
                throw new InvalidOperationException($"Duplicate variable alias '{alias}'.");

            var variable = new FormVariable { Alias = alias };
            configure?.Invoke(new VariableBuilder(variable));
            _variables.Add(variable);
            return this;
        }

        /// <summary>
        /// Adds calculation rules that change a variable. All rules run in the order they're added, across variables, so a rule sees
        /// what the rules before it did.
        /// </summary>
        public FormBuilder Calculate(string variableAlias, Action<CalculationBuilder> configure)
        {
            configure(new CalculationBuilder(variableAlias, _calculations));
            return this;
        }

        public FormBuilder OfType(string typeAlias, object settings)
        {
            _type = new FormDefinitionTypeReference
            {
                TypeAlias = typeAlias,
                Settings = settings
            };
            return this;
        }

        public FormBuilder OnSubmit(Action<WorkflowBuilder> configure)
        {
            var workflowBuilder = new WorkflowBuilder();
            configure(workflowBuilder);
            _workflows.AddRange(workflowBuilder.Build());
            return this;
        }

        public FormDefinition Build()
        {
            var definition = new FormDefinition
            {
                Fields = _fields,
                Pages = _pages,
                Workflows = _workflows,
                SubmitLabel = _submitLabel,
                ShowProgress = _showProgress,
                Variables = _variables,
                Calculations = _calculations,
                ConditionalOutcomes = _conditionalOutcomes
            };
            if (_type != null)
            {
                definition.Type = _type;
            }
            foreach (var field in definition.GetAllFields().Where(it => it.Extension != null))
            {
                field.Extension!.FormTypeAlias = definition.Type.TypeAlias;
            }
            if (_outcome != null)
            {
                definition.SubmitOutcome = _outcome;
            }
            return definition;
        }
    }
}
