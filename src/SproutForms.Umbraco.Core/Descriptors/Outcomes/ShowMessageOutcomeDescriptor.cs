using SproutForms.Core.Models.Outcomes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Descriptors.Outcomes
{
    public class ShowMessageOutcomeDescriptor : BaseOutcomeDescriptor<ShowMessageOutcomeConfig>
    {
        public override string OutcomeTypeAlias => "message";

        public override string DisplayName => "Show message";

        public override string Description => "Show a message when the user submits the form";

        public ShowMessageOutcomeDescriptor()
        {
            DefineMap(it => it.Message, "message", "Message", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
