using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Models.SubmissionGuard;
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
        private readonly IEnumerable<IFormSubmitOutcomeType> _outcomeTypes;
        private readonly IFormSubmissionGuard? _formSubmissionGuard;

        public FormSubmissionController(
            IFormVersionRepository formVersions,
            IFormSubmissionService submissionService,
            FormRenderingService formRenderingService,
            IEnumerable<IFormSubmitOutcomeType> outcomeTypes,
            IFormSubmissionGuard? formSubmissionGuard)
        {
            _formVersions = formVersions;
            _submissionService = submissionService;
            _formRenderingService = formRenderingService;
            _outcomeTypes = outcomeTypes;
            _formSubmissionGuard = formSubmissionGuard;
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

            if (_formSubmissionGuard != null)
            {
                var submissionGuardResult = await _formSubmissionGuard.EvaluateAsync(values);
                if (!submissionGuardResult.Allowed)
                {
                    var errors = new Dictionary<string, List<string>>
                {
                    { "submissionGuard", new List<string> { submissionGuardResult.ErrorMessage! } }
                };
                    if (IsAjaxRequest(Request))
                    {
                        return BadRequest(new AjaxFormResponse
                        {
                            Success = false,
                            Errors = errors,
                            Values = values
                        });
                    }
                    TempData[$"{formVersion.FormId}:FormErrors"] = JsonSerializer.Serialize(errors);
                    TempData[$"{formVersion.FormId}:FormValues"] = JsonSerializer.Serialize(values);
                    return Redirect(Request.Headers["Referer"].ToString());
                }

                foreach (var guardField in _formSubmissionGuard.GetFormFields())
                    values.Remove(guardField.Name);
            }

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

            values.TryGetValue("sf_PageUrl", out var pageUrl);
            var request = new FormSubmissionRequest
            {
                Values = values.Where(it => formVersion.Definition.Fields.Any(f => f.Alias == it.Key)).ToDictionary(
                         kvp => kvp.Key,
                         kvp => JsonSerializer.SerializeToElement(kvp.Value)),
                PageUrl = pageUrl
            };

            var result = await _submissionService.SubmitAsync(formVersion, request);
            if (result is null)
                return NotFound();

            var outcomeType = _outcomeTypes.First(it => it.Alias == formVersion.Definition.SubmitOutcome.OutcomeTypeAlias);
            var outcomeResult = outcomeType.Handle(formVersion.Definition.SubmitOutcome.Configuration);
            outcomeResult.OutcomeTypeAlias = outcomeType.Alias;

            if (IsAjaxRequest(Request))
            {
                if (result.IsValid)
                {
                    var responseModel = new AjaxFormResponse
                    {
                        Success = true,
                        OutcomeType = outcomeResult.OutcomeTypeAlias,
                        OutcomeData = outcomeResult.Data
                    };

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
                if (outcomeResult.Data.TryGetValue("url", out var redirectUrl) && redirectUrl is string url && !string.IsNullOrWhiteSpace(url))
                    return Redirect(url);
                else if (outcomeResult.Data.TryGetValue("message", out var message) && message is string msg && !string.IsNullOrWhiteSpace(msg))
                    TempData[$"{formVersion.FormId}:SuccessMessage"] = msg;

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
