using SproutForms.Core.Models;
using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.Conditions;
using System.Text.Json;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Works out a form's variables from the answers by running its calculation rules top to bottom, the same way the browser does.
    /// A field the visitor doesn't see, because of its own conditions or a skipped page, counts as empty.
    /// </summary>
    public class FormCalculator
    {
        private static readonly IReadOnlyDictionary<string, JsonElement> NoVariables = new Dictionary<string, JsonElement>();

        private readonly IConditionEvaluator _conditionEvaluator;

        public FormCalculator(IConditionEvaluator conditionEvaluator)
        {
            _conditionEvaluator = conditionEvaluator;
        }

        public IReadOnlyDictionary<string, JsonElement> Calculate(FormDefinition definition, IReadOnlyDictionary<string, JsonElement> values)
        {
            if (definition.Variables.Count == 0)
                return NoVariables;

            // What's visible can depend on the variables, and the variables on what's visible. The structure validator rules out
            // cycles, so each pass settles at least one more variable, and it's done once a pass changes nothing
            var dependsOnVisibility = new CalculationDependencies(definition).FormConditionsUseVariables();
            var variables = Run(definition, WithoutHiddenFields(definition, values, Initial(definition)));
            for (var pass = 0; dependsOnVisibility && pass < definition.Variables.Count; pass++)
            {
                var next = Run(definition, WithoutHiddenFields(definition, values, variables));
                if (AreEqual(next, variables))
                    break;
                variables = next;
            }
            return variables;
        }

        private static Dictionary<string, JsonElement> Initial(FormDefinition definition)
            => definition.Variables
                .GroupBy(variable => variable.Alias)
                .ToDictionary(group => group.Key, group => VariableValues.Initial(group.First()));

        private Dictionary<string, JsonElement> WithoutHiddenFields(FormDefinition definition, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement> variables)
        {
            var visible = new Dictionary<string, JsonElement>(values);
            foreach (var page in definition.Pages.Where(page => !_conditionEvaluator.IsVisible(page, values, variables)))
            {
                foreach (var column in page.Rows.SelectMany(row => row.Columns))
                    visible.Remove(column.FieldAlias);
            }
            foreach (var field in definition.Fields.Where(field => !_conditionEvaluator.IsVisible(field, values, variables)))
            {
                visible.Remove(field.Alias);
            }
            return visible;
        }

        private Dictionary<string, JsonElement> Run(FormDefinition definition, IReadOnlyDictionary<string, JsonElement> values)
        {
            var variables = Initial(definition);
            var types = definition.Variables.GroupBy(variable => variable.Alias).ToDictionary(group => group.Key, group => group.First());

            foreach (var rule in definition.Calculations)
            {
                if (!types.TryGetValue(rule.VariableAlias, out var variable))
                    continue;
                if (!_conditionEvaluator.Evaluate(rule.Condition, values, variables))
                    continue;

                var operand = Resolve(rule.Operand, values, variables);
                variables[variable.Alias] = Apply(variable, variables[variable.Alias], rule.Operation, operand);
            }

            foreach (var variable in types.Values.Where(variable => variable.Type == FormVariableType.Number))
            {
                variables[variable.Alias] = VariableValues.FromNumber(VariableValues.Round(VariableValues.ToNumber(variables[variable.Alias]), variable.Decimals));
            }
            return variables;
        }

        private static JsonElement Resolve(CalculationOperand operand, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement> variables)
        {
            var alias = CalculationDependencies.Alias(operand.Value);
            return operand.Source switch
            {
                ConditionValueSource.Field => alias is not null && values.TryGetValue(alias, out var value) ? value : default,
                ConditionValueSource.Variable => alias is not null && variables.TryGetValue(alias, out var variable) ? variable : default,
                _ => operand.Value is JsonElement element ? element : JsonSerializer.SerializeToElement(operand.Value)
            };
        }

        // An operation that doesn't fit the variable's type, or a number that overflows or is divided by zero, leaves it as it was
        private static JsonElement Apply(FormVariable variable, JsonElement current, CalculationOperation operation, JsonElement operand)
        {
            if (variable.Type == FormVariableType.Text)
            {
                return operation switch
                {
                    CalculationOperation.Set => VariableValues.FromText(VariableValues.ToText(operand)),
                    CalculationOperation.Append => VariableValues.FromText(VariableValues.ToText(current) + VariableValues.ToText(operand)),
                    _ => current
                };
            }

            var number = VariableValues.ToNumber(current);
            var by = VariableValues.ToNumber(operand);
            try
            {
                return operation switch
                {
                    CalculationOperation.Set => VariableValues.FromNumber(by),
                    CalculationOperation.Add => VariableValues.FromNumber(number + by),
                    CalculationOperation.Subtract => VariableValues.FromNumber(number - by),
                    CalculationOperation.Multiply => VariableValues.FromNumber(number * by),
                    CalculationOperation.Divide when by != 0 => VariableValues.FromNumber(number / by),
                    _ => current
                };
            }
            catch (OverflowException)
            {
                return current;
            }
        }

        private static bool AreEqual(IReadOnlyDictionary<string, JsonElement> left, IReadOnlyDictionary<string, JsonElement> right)
            => left.Count == right.Count
                && left.All(it => right.TryGetValue(it.Key, out var other) && it.Value.GetRawText() == other.GetRawText());
    }
}
