using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class FormSubmission
    {
        public Guid Id { get; set; }
        public Guid FormVersionId { get; set; }

        public DateTime SubmittedAt { get; set; }
        public string? IpAddress { get; set; }

        public IReadOnlyDictionary<string, JsonElement> Values { get; init; } = new Dictionary<string, JsonElement>();
    }
}
