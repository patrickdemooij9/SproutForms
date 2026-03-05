using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SproutForms.Core.Flows.Models
{
    public class TeamsMessageModel
    {
        [JsonPropertyName("type")]
        public string Type => "message";

        [JsonPropertyName("attachments")]
        public TeamsAttachmentModel[] Attachments { get; set; } = [];
    }
}
