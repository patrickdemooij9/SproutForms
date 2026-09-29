using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.TagHelpers
{
    public class RenderFormViewComponent : ViewComponent
    {
        private readonly IFormRepository _formRepository;
        private readonly IFormVersionRepository _formVersionRepository;
        private readonly FormRenderingService _formRenderingService;
        private readonly FormThemeViewResolver _themeViewResolver;
        private readonly ILogger<RenderFormViewComponent> _logger;

        public RenderFormViewComponent(IFormRepository formRepository, IFormVersionRepository formVersionRepository, FormRenderingService formRenderingService, FormThemeViewResolver themeViewResolver, ILogger<RenderFormViewComponent> logger)
        {
            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
            _formRenderingService = formRenderingService;
            _themeViewResolver = themeViewResolver;
            _logger = logger;
        }

        /// <param name="theme">A folder under ~/Views/Forms/Themes/ with the views to use instead of the default ones. Defaults to SproutForms:DefaultTheme.</param>
        public async Task<IViewComponentResult> InvokeAsync(string? formAlias = null, Guid? formId = null, string? theme = null)
        {
            Form? form = null;
            if (!string.IsNullOrWhiteSpace(formAlias))
            {
                form = _formRepository.GetByAlias(formAlias);
            }
            else if (formId.HasValue)
            {
                form = _formRepository.GetById(formId.Value);
            }

            // A form in the recycle bin is shown as if it was deleted
            if (form is null || form.IsTrashed)
            {
                _logger.LogWarning("Form {FormReference} doesn't exist or is in the recycle bin, so nothing is rendered", formAlias ?? formId?.ToString());
                return Content(string.Empty);
            }
            var formVersion = _formVersionRepository.GetPublished(form.Id);

            var model = _formRenderingService.Build(form, formVersion!);
            var resolvedTheme = _themeViewResolver.GetTheme(theme);
            // The views' partials read the theme from here, so they fall back per file too
            ViewData[FormThemeViewResolver.ViewDataKey] = resolvedTheme;
            return View(_themeViewResolver.GetViewPath(resolvedTheme, "Form"), model);
        }
    }
}
