using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models.Conditions
{
    public class ConditionDefinition
    {
        public string Operator { get; init; } = "All"; // All / Any
        public List<ConditionRule> Rules { get; init; } = new();
    }
}
