using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.FormTypes;
using System.Text.Json;

namespace SproutForms.Core.Builders
{
    public class FieldBuilder<TConfig, TValue> where TConfig: class
    {
        private readonly ColumnBuilder _column;
        private readonly FormField _field;
        private readonly TConfig _config;

        internal FieldBuilder(
            ColumnBuilder column,
            FormField field,
            TConfig config)
        {
            _column = column;
            _field = field;
            _config = config;
        }

        public FieldBuilder<TConfig, TValue> Required()
        {
            _field.Required = true;
            return this;
        }

        /// <summary>
        /// Shows the field only when the condition holds, or that of another VisibleWhen. A hidden field isn't validated, and counts
        /// as empty in calculations.
        /// </summary>
        public FieldBuilder<TConfig, TValue> VisibleWhen(Action<ConditionBuilder> condition)
            => AddRule(FieldRuleAction.Show, condition);

        /// <summary>
        /// Hides the field when the condition holds, whatever its VisibleWhen rules say.
        /// </summary>
        public FieldBuilder<TConfig, TValue> HiddenWhen(Action<ConditionBuilder> condition)
            => AddRule(FieldRuleAction.Hide, condition);

        /// <summary>
        /// Makes the field required when the condition holds.
        /// </summary>
        public FieldBuilder<TConfig, TValue> RequiredWhen(Action<ConditionBuilder> condition)
            => AddRule(FieldRuleAction.Require, condition);

        private FieldBuilder<TConfig, TValue> AddRule(FieldRuleAction action, Action<ConditionBuilder> condition)
        {
            _field.Rules.Add(new FieldRule
            {
                Condition = ConditionBuilder.Build(condition),
                Action = action
            });
            return this;
        }

        public FieldBuilder<TConfig, TValue> Set(Action<TConfig> configurate)
        {
            configurate(_config);
            _field.Configuration = _config;
            return this;
        }

        /// <summary>
        /// Sets the settings the form's type adds to this field's type, such as the correct answer in a quiz.
        /// </summary>
        public FieldBuilder<TConfig, TValue> Extend(object settings)
        {
            // FormBuilder.Build fills in the form type, which can be set after the fields
            _field.Extension = new FormFieldExtensionValue
            {
                FormTypeAlias = string.Empty,
                Settings = settings
            };
            return this;
        }

        public ColumnBuilder Done()
        {
            _column.SetField(_field);
            return _column;
        }

        public TConfig Config => _config;
    }
}
