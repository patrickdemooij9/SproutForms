using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
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

        public RenderFormViewComponent(IFormRepository formRepository, IFormVersionRepository formVersionRepository, FormRenderingService formRenderingService)
        {
            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
            _formRenderingService = formRenderingService;
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

            if (form is null)
            {
                return null;
            }
            var formVersion = _formVersionRepository.GetPublished(form.Id);

            var model = _formRenderingService.Build(formVersion!);
            return View("~/Views/Forms/Form.cshtml", model);
        }
    }
}
