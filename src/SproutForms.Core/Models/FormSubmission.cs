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
        public string? PageUrl { get; set; }

        public IReadOnlyDictionary<string, JsonElement> Values { get; init; } = new Dictionary<string, JsonElement>();

        // What the form's type computed from the values, such as a quiz score
        public IReadOnlyDictionary<string, JsonElement> Results { get; init; } = new Dictionary<string, JsonElement>();

        // Set while the submission is in its form's recycle bin, where it is treated as deleted everywhere but in the bin itself
        public DateTime? TrashedAt { get; set; }
        public string? TrashedBy { get; set; }
        public bool IsTrashed => TrashedAt.HasValue;
    }
}
