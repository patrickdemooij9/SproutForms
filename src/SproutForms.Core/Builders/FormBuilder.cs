using SproutForms.Core.Models;
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
        private readonly List<FormWorkflow> _workflows = [];
        private FormSubmitOutcome? _outcome;
        private FormDefinitionTypeReference? _type;

        public string Alias { get; }
        public string Name { get; }

        public FormBuilder(string alias, string name)
        {
            Alias = alias;
            Name = name;
        }

        internal FormField RegisterField(FormField field)
        {
            if (_fields.Any(f => f.Alias == field.Alias))
                throw new InvalidOperationException($"Duplicate field alias '{field.Alias}'.");

            _fields.Add(field);
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
                SubmitLabel = _submitLabel
            };
            if (_type != null)
            {
                definition.Type = _type;
            }
            foreach (var field in _fields.Where(it => it.Extension != null))
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
