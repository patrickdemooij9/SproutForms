using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Umbraco.Core.Services;

namespace SproutForms.Umbraco.Core.Helpers
{
    public static class FormThemeHtmlHelperExtensions
    {
        /// <summary>
        /// Renders a SproutForms view in the current form's theme, falling back to the default view when the theme doesn't have it.
        /// </summary>
        /// <param name="view">The view's path under ~/Views/Forms/ without the extension, such as "Rows" or "Fields/text".</param>
        public static Task<IHtmlContent> SproutFormsPartialAsync(this IHtmlHelper html, string view, object? model)
        {
            var resolver = html.ViewContext.HttpContext.RequestServices.GetRequiredService<FormThemeViewResolver>();
            return html.PartialAsync(resolver.GetViewPath(html.GetSproutFormsTheme(), view), model);
        }

        /// <summary>
        /// The theme the current form renders with, or null for the default views.
        /// </summary>
        public static string? GetSproutFormsTheme(this IHtmlHelper html)
            => html.ViewData[FormThemeViewResolver.ViewDataKey] as string;
    }
}
