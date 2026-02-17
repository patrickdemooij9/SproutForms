using SproutForms.Umbraco.Core.Models.ViewModels;

namespace SproutForms.Umbraco.Core.Descriptors.Flows
{
    public interface IFlowDescriptor
    {
        string FlowTypeAlias { get; }

        string DisplayName { get; }
        string DisplayTemplate { get; }
        string Description { get; }

        FormPropertyBackofficeModel[] FromConfig(object configuration);
        object ToConfig(Dictionary<string, object?> properties);
    }
}
