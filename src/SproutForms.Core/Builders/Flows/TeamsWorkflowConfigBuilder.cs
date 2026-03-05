using SproutForms.Core.Flows.Configs;

namespace SproutForms.Core.Builders.Flows
{
    public class TeamsWorkflowConfigBuilder
    {
        private readonly TeamsWorkflowConfig _config = new();

        public TeamsWorkflowConfigBuilder WebhookUrl(string webhookUrl)
        {
            _config.WebhookUrl = webhookUrl;
            return this;
        }

        public TeamsWorkflowConfigBuilder Message(string message)
        {
            _config.Message = message;
            return this;
        }

        public TeamsWorkflowConfigBuilder ThemeColor(string themeColor)
        {
            _config.ThemeColor = themeColor;
            return this;
        }

        internal TeamsWorkflowConfig Build() => _config;
    }
}
