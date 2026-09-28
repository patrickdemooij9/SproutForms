using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SproutForms.Umbraco.Core.ViewComponents;

/// <summary>
/// Tag helper that renders the required CSS and JavaScript dependencies for SproutForms.
/// </summary>
[HtmlTargetElement("render-form-dependencies")]
public class RenderFormDependenciesTagHelper : TagHelper
{
    /// <summary>
    /// Renders the stylesheet for the grid, conditional fields and pages. Leave it out only when the site styles those itself.
    /// </summary>
    public bool IncludeLayout { get; set; } = true;

    /// <summary>
    /// Renders the default look. Turn it off to style the forms with the site's own CSS.
    /// </summary>
    public bool IncludeTheme { get; set; } = true;

    /// <summary>
    /// Renders forms.js, which adds validation, conditions, pages and submitting without a page reload.
    /// </summary>
    public bool IncludeScripts { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Suppress the tag itself - we only want to output the dependencies
        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;

        var tags = new List<string>();
        if (IncludeLayout)
        {
            tags.Add(@"<link rel=""stylesheet"" href=""/forms/forms-layout.css"" />");
        }
        if (IncludeTheme)
        {
            tags.Add(@"<link rel=""stylesheet"" href=""/forms/forms-default-theme.css"" />");
        }
        if (IncludeScripts)
        {
            tags.Add(@"<script src=""/forms/forms.js""></script>");
        }

        output.Content.SetHtmlContent(string.Join(Environment.NewLine, tags));
    }
}
