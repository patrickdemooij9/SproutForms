using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Models.Flows;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Builders.Flows
{
    public sealed class EmailWorkflowConfigBuilder
    {
        private readonly EmailWorkflowConfig _config = new();

        public EmailWorkflowConfigBuilder To(string to)
        {
            _config.To = to;
            return this;
        }

        public EmailWorkflowConfigBuilder From(string from)
        {
            _config.From = from;
            return this;
        }

        public EmailWorkflowConfigBuilder Subject(string subject)
        {
            _config.Subject = subject;
            return this;
        }

        internal EmailWorkflowConfig Build()
            => _config;
    }

}
