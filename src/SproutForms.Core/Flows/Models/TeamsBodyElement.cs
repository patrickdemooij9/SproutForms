using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SproutForms.Core.Flows.Models
{
    [JsonDerivedType(typeof(TeamsTextBlockModel))]
    public abstract class TeamsBodyElement
    {
    }
}
