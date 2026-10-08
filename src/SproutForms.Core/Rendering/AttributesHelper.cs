using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Text.Encodings.Web;

namespace SproutForms.Core.Rendering
{
    public static class AttributesHelper
    {
        public static IHtmlContent RenderAttributes(IDictionary<string, string> attributes)
        {
            var builder = new HtmlContentBuilder();

            foreach (var attr in attributes)
            {
                if (!string.IsNullOrWhiteSpace(attr.Value))
                {
                    builder.AppendHtml($" {attr.Key}=\"{HtmlEncoder.Default.Encode(attr.Value)}\"");
                }
            }

            return builder;
        }

        /// <summary>
        /// The attributes of a field's wrapper: forms.js finds the field by its path, and the stylesheets look at its type. Its rules
        /// come with the form's definition.
        /// </summary>
        public static IDictionary<string, string> Build(FormFieldViewModel model)
            => new Dictionary<string, string>
            {
                ["data-sf-field-id"] = model.Alias,
                ["data-sf-field-type"] = model.Type,
            };
    }
}
