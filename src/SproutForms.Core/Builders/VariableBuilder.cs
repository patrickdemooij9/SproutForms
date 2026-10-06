using SproutForms.Core.Models.Calculations;

namespace SproutForms.Core.Builders
{
    public class VariableBuilder
    {
        private readonly FormVariable _variable;

        internal VariableBuilder(FormVariable variable)
        {
            _variable = variable;
        }

        public VariableBuilder Label(string label)
        {
            _variable.Label = label;
            return this;
        }

        /// <summary>
        /// A number, rounded to this many decimals once the calculations are done. Variables are numbers unless they're made text.
        /// </summary>
        public VariableBuilder Number(int decimals = 0)
        {
            _variable.Type = FormVariableType.Number;
            _variable.Decimals = decimals;
            return this;
        }

        public VariableBuilder Text()
        {
            _variable.Type = FormVariableType.Text;
            return this;
        }

        public VariableBuilder StartAt(object initialValue)
        {
            _variable.InitialValue = initialValue;
            return this;
        }
    }
}
