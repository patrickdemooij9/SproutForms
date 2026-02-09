using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SproutForms.Umbraco.Core.ViewComponents;

/// <summary>
/// Tag helper that renders the required CSS and JavaScript dependencies for SproutForms.
/// </summary>
[HtmlTargetElement("render-form-dependencies")]
public class RenderFormDependenciesTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Suppress the tag itself - we only want to output the dependencies
        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;

        // Create the CSS link tags
        var cssLinksHtml = @"<link rel=""stylesheet"" href=""/forms/forms-layout.css"" />
        <link rel=""stylesheet"" href=""/forms/forms-default-theme.css"" />";

        // Create the JavaScript script tag
        var scriptHtml = @"<script src=""/forms/forms.js""></script>";

        // Combine and set the content
        var html = $"{cssLinksHtml}{Environment.NewLine}        {scriptHtml}";
        
        output.Content.SetHtmlContent(html);
    }
}
