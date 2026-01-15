using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Models.ViewModels;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Umbraco.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Controllers
{
    [ApiController]
    [Route("api/forms")]
    public class FormSubmissionController : Controller
    {
        private readonly IFormVersionRepository _formVersions;
        private readonly IFormSubmissionService _submissionService;
        private readonly FormRenderingService _formRenderingService;

        public FormSubmissionController(
            IFormVersionRepository formVersions,
            IFormSubmissionService submissionService,
            FormRenderingService formRenderingService)
        {
            _formVersions = formVersions;
            _submissionService = submissionService;
            _formRenderingService = formRenderingService;
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            Guid id,
            [FromForm] Dictionary<string, string> values)
        {
            var formVersion = _formVersions.GetPublished(id);
            if (formVersion is null)
                return NotFound();

            if (Request.Form.Files.Any())
            {
                var fileResults = await _submissionService.HandleFileSubmits(formVersion, [.. Request.Form.Files]);
                if (fileResults.Errors.Any())
                {
                    if (IsAjaxRequest(Request))
                    {
                        return BadRequest(new AjaxFormResponse
                        {
                            Success = false,
                            Errors = fileResults.Errors,
                            Values = values
                        });
                    }
                    TempData[$"{formVersion.FormId}:FormErrors"] = JsonSerializer.Serialize(fileResults.Errors);
                    TempData[$"{formVersion.FormId}:FormValues"] = JsonSerializer.Serialize(values);
                    return Redirect(Request.Headers["Referer"].ToString());
                }
                foreach (var fileResult in fileResults.Values)
                {
                    values[fileResult.Key] = fileResult.Value;
                }
            }

            var request = new FormSubmissionRequest
            {
                Values = values.ToDictionary(
                         kvp => kvp.Key,
                         kvp => JsonSerializer.SerializeToElement(kvp.Value))
            };

            var result = await _submissionService.SubmitAsync(formVersion, request);
            if (result is null)
                return NotFound();

            if (IsAjaxRequest(Request))
            {
                if (result.IsValid)
                {
                    var responseModel = new AjaxFormResponse { Success = true };
                    if (formVersion.Definition.SubmitOutcome.OutcomeTypeAlias == RedirectUrlOutcomeType.Alias)
                    {
                        var config = (RedirectUrlOutcomeConfig)formVersion.Definition.SubmitOutcome.Configuration;
                        responseModel.RedirectUrl = config.RedirectUrl;
                    }
                    else if (formVersion.Definition.SubmitOutcome.OutcomeTypeAlias == ShowMessageOutcome.Alias)
                    {
                        var config = (ShowMessageOutcomeConfig)formVersion.Definition.SubmitOutcome.Configuration;
                        responseModel.SuccessMessage = config.Message;
                    }
                    return Ok(responseModel);
                }
                    

                return BadRequest(new AjaxFormResponse
                {
                    Success = false,
                    Errors = result.Errors,
                    Values = values
                });
            }

            if (result.IsValid)
            {
                if (formVersion.Definition.SubmitOutcome.OutcomeTypeAlias == RedirectUrlOutcomeType.Alias)
                {
                    var config = (RedirectUrlOutcomeConfig)formVersion.Definition.SubmitOutcome.Configuration;
                    return Redirect(config.RedirectUrl);
                }
                else if (formVersion.Definition.SubmitOutcome.OutcomeTypeAlias == ShowMessageOutcome.Alias)
                {
                    var config = (ShowMessageOutcomeConfig)formVersion.Definition.SubmitOutcome.Configuration;
                    TempData[$"{formVersion.FormId}:SuccessMessage"] = config.Message;
                }
                return Redirect(Request.Headers["Referer"].ToString());
            }

            TempData[$"{formVersion.FormId}:FormErrors"] = JsonSerializer.Serialize(result.Errors);
            TempData[$"{formVersion.FormId}:FormValues"] = JsonSerializer.Serialize(result.Values);

            return Redirect(Request.Headers["Referer"].ToString());
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || request.Headers["Accept"].Any(x => x.Contains("application/json"));
        }

        //TODO: Add endpoint for headless use
    }
}
