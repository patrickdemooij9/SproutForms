using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SproutForms.Core.Flows.Models
{
    public class TeamsAttachmentModel
    {
        [JsonPropertyName("contentType")]
        public string ContentType => "application/vnd.microsoft.card.adaptive";

        [JsonPropertyName("content")]
        public TeamsAdaptiveCardModel Content { get; set; }
    }
}
