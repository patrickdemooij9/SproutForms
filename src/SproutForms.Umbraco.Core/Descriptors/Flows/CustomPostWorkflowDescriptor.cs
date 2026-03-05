using SproutForms.Core.Flows.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Flows
{
    public class CustomPostWorkflowDescriptor : BaseFlowDescriptor<CustomPostWorkflowConfig>
    {
        public override string FlowTypeAlias => "customPost";

        public override string DisplayTemplate => "Post form data to {url}";

        public override string DisplayName => "Post to custom endpoint";

        public override string Description => "Sends form submission data as JSON to a custom URL after submission.";

        public CustomPostWorkflowDescriptor()
        {
            DefineMap(it => it.Url, "url", "URL", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.Method, "method", "Method", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
