using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models.SubmissionGuard
{
    public class NoFormSubmissionGuard : IFormSubmissionGuard
    {
        public string Alias => "none";

        public Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues)
        {
            return Task.FromResult(new SubmissionGuardResult() { Allowed = true });
        }

        public object? GetFrontendSettings()
        {
            return null;
        }
    }
}
