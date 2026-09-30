using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class FormSubmissionRequest
    {
        // The name of the posted value that holds the page the form was on, in a Razor post and in a headless submission
        public const string PageUrlKey = "sf_PageUrl";

        public Dictionary<string, JsonElement> Values { get; init; } = new();
        public string? PageUrl { get; init; }
    }
}
