using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class KeyValuePairModel
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
