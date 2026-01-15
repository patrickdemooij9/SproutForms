using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Outcomes;
using System.Text.Json;

namespace SproutForms.Core.Builders
{
    public class FormBuilder
    {
        private readonly List<FormField> _fields = [];
        private readonly List<FormRow> _rows = [];
        private readonly List<FormWorkflow> _workflows = [];
        private FormSubmitOutcome? _outcome;

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

        public FormBuilder Row(Action<RowBuilder> configure)
        {
            var row = new RowBuilder(this);
            configure(row);
            _rows.Add(row.Build());
            return this;
        }

        internal FormBuilder SetOutcome(IFormSubmitOutcomeType outcome, object configuration)
        {
            _outcome = new FormSubmitOutcome
            {
                OutcomeTypeAlias = outcome.Alias,
                Configuration = configuration
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
                Rows = _rows,
                Workflows = _workflows
            };
            if (_outcome != null)
            {
                definition.SubmitOutcome = _outcome;
            }
            return definition;
        }
    }
}
