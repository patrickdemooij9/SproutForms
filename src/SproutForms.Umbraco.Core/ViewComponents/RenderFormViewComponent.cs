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
        private readonly ILogger<RenderFormViewComponent> _logger;

        public RenderFormViewComponent(IFormRepository formRepository, IFormVersionRepository formVersionRepository, FormRenderingService formRenderingService, ILogger<RenderFormViewComponent> logger)
        {
            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
            _formRenderingService = formRenderingService;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? formAlias = null, Guid? formId = null)
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

            var model = _formRenderingService.Build(formVersion!);
            return View("~/Views/Forms/Form.cshtml", model);
        }
    }
}
