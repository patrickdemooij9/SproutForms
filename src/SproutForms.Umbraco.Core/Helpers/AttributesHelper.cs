using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace SproutForms.Umbraco.Core.Helpers
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

        public static IDictionary<string, string> Build(FormFieldViewModel model)
        {
            var attributes = new Dictionary<string, string>
            {
                ["data-sf-field-id"] = model.Alias,
                ["data-sf-field-type"] = model.Type,
            };

            if (model.ValidationRules.Count() > 0) //TODO: Fix multiple enumerations
            {
                attributes["data-sf-validate"] = string.Join(",", model.ValidationRules.Select(it => it.Type));

                foreach (var validationRule in model.ValidationRules)
                {
                    var typeKebab = validationRule.Type.PascalToKebabCase();

                    attributes[$"data-sf-{typeKebab}"] = validationRule.Value?.ToString();
                    attributes[$"data-sf-{typeKebab}-message"] = validationRule.Message;
                }
            }

            return attributes;
        }

        public static string PascalToKebabCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(
                value,
                "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])",
                "-$1",
                RegexOptions.Compiled)
                .Trim()
                .ToLower();
        }
    }
}
