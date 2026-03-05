using SproutForms.Core.Flows.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Flows
{
    public class TeamsWorkflowDescriptor : BaseFlowDescriptor<TeamsWorkflowConfig>
    {
        public override string FlowTypeAlias => "teams";

        public override string DisplayTemplate => "Send a message to Microsoft Teams";

        public override string DisplayName => "Send to Microsoft Teams";

        public override string Description => "Sends a message to a Microsoft Teams channel after form submission.";

        public TeamsWorkflowDescriptor()
        {
            DefineMap(it => it.WebhookUrl, "webhookUrl", "Webhook URL", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.Message, "message", "Message", "sproutForms.propertyEditorUi.tokenTextarea");
        }
    }
}
