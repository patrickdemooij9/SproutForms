using SproutForms.Core.Flows.Configs;

namespace SproutForms.Core.Builders.Flows
{
    public class CustomPostWorkflowConfigBuilder
    {
        private readonly CustomPostWorkflowConfig _config = new();

        public CustomPostWorkflowConfigBuilder Url(string url)
        {
            _config.Url = url;
            return this;
        }

        public CustomPostWorkflowConfigBuilder Method(string method)
        {
            _config.Method = method;
            return this;
        }

        internal CustomPostWorkflowConfig Build() => _config;
    }
}
