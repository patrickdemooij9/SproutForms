using SproutForms.Umbraco.Core.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models.Outcomes
{
    public class ShowMessageOutcomeConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string Message { get; set; }
    }
}
