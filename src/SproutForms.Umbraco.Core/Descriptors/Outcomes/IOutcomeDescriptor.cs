using SproutForms.Umbraco.Core.Models.ViewModels;

namespace SproutForms.Umbraco.Core.Descriptors.Outcomes
{
    public interface IOutcomeDescriptor
    {
        string OutcomeTypeAlias { get; }

        string DisplayName { get; }
        string Description { get; }

        FormPropertyBackofficeModel[] FromConfig(object configuration);
        object ToConfig(Dictionary<string, object?> properties);
    }
}
