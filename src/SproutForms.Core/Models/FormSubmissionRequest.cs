using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class FormSubmissionRequest
    {
        public Dictionary<string, JsonElement> Values { get; init; } = new();
        public string? PageUrl { get; init; }
    }
}
