using SproutForms.Core.Flows.Configs;

namespace SproutForms.Core.Builders.Flows
{
    public class SlackWorkflowConfigBuilder
    {
        private readonly SlackWorkflowConfig _config = new();

        public SlackWorkflowConfigBuilder WebhookUrl(string webhookUrl)
        {
            _config.WebhookUrl = webhookUrl;
            return this;
        }

        public SlackWorkflowConfigBuilder Message(string message)
        {
            _config.Message = message;
            return this;
        }

        internal SlackWorkflowConfig Build() => _config;
    }
}
