using SproutForms.Core.Flows.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Flows
{
    public class SlackWorkflowDescriptor : BaseFlowDescriptor<SlackWorkflowConfig>
    {
        public override string FlowTypeAlias => "slack";

        public override string DisplayTemplate => "Send a message to Slack";

        public override string DisplayName => "Send to Slack";

        public override string Description => "Sends a message to a Slack channel after form submission.";

        public SlackWorkflowDescriptor()
        {
            DefineMap(it => it.WebhookUrl, "webhookUrl", "Webhook URL", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.Message, "message", "Message", "sproutForms.propertyEditorUi.tokenTextarea");
        }
    }
}
