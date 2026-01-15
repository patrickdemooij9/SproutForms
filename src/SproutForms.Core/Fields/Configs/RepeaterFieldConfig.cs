using SproutForms.Core.Models;

namespace SproutForms.Core.Fields.Configs
{
    public class RepeaterFieldConfig
    {
        public required IReadOnlyList<FormField> Fields { get; set; }

        public int? MinItems { get; set; }
        public int? MaxItems { get; set; }
    }
}
