using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SproutForms.Core;

namespace SproutForms.Core.Rendering
{
    /// <summary>
    /// Finds the view a form renders with. A theme is a folder under ~/Views/Forms/Themes/ that only holds the views it changes:
    /// every view it doesn't have falls back to the one in ~/Views/Forms/.
    /// </summary>
    public class FormThemeViewResolver
    {
        public const string ViewDataKey = "SproutForms.Theme";

        private const string DefaultViewRoot = "~/Views/Forms/";
        private const string ThemesRoot = "~/Views/Forms/Themes/";

        // The theme becomes part of a path, so it may only be a folder name
        private static readonly Regex ValidThemeName = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

        private readonly ICompositeViewEngine _viewEngine;
        private readonly IOptionsMonitor<SproutFormsOptions> _options;
        private readonly ILogger<FormThemeViewResolver> _logger;

        public FormThemeViewResolver(ICompositeViewEngine viewEngine, IOptionsMonitor<SproutFormsOptions> options, ILogger<FormThemeViewResolver> logger)
        {
            _viewEngine = viewEngine;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// The theme to render with: the one asked for, otherwise SproutForms:DefaultTheme. Null means the default views.
        /// </summary>
        public string? GetTheme(string? requestedTheme)
        {
            var theme = string.IsNullOrWhiteSpace(requestedTheme) ? _options.CurrentValue.DefaultTheme : requestedTheme;
            if (string.IsNullOrWhiteSpace(theme)) return null;

            if (!ValidThemeName.IsMatch(theme))
            {
                _logger.LogWarning("Form theme {Theme} isn't a valid folder name, so the default views are used", theme);
                return null;
            }
            return theme;
        }

        /// <param name="theme">A theme from <see cref="GetTheme"/>.</param>
        /// <param name="view">The view's path under ~/Views/Forms/ without the extension, such as "Form" or "Fields/text".</param>
        public string GetViewPath(string? theme, string view)
        {
            if (theme is not null)
            {
                var themedPath = $"{ThemesRoot}{theme}/{view}.cshtml";
                // The view engine caches lookups, and clears them when a view is added with runtime compilation
                if (_viewEngine.GetView(executingFilePath: null, viewPath: themedPath, isMainPage: false).Success)
                {
                    return themedPath;
                }
            }
            return $"{DefaultViewRoot}{view}.cshtml";
        }
    }
}
