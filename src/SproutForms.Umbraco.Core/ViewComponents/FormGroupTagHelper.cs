using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.ViewComponents
{
    [HtmlTargetElement("sf-form-group")]
    public class FormGroupTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.Add("data-sf-form-id", "Test");
        }
    }
}
