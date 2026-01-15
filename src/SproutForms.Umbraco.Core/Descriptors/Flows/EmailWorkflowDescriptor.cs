using SproutForms.Core.Flows.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Flows
{
    public class EmailWorkflowDescriptor : BaseFlowDescriptor<EmailWorkflowConfig>
    {
        public override string FlowTypeAlias => "email";

        public override string DisplayName => "Send an email";

        public override string Description => "Sends an email after the form is submitted.";

        public EmailWorkflowDescriptor()
        {
            DefineMap(it => it.To, "to", "To", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.From, "from", "From", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.Subject, "subject", "Subject", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
