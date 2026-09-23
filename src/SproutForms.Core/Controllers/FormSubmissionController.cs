using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Core.Models.ViewModels;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using System.Text.Json;

namespace SproutForms.Core.Controllers
{
    [ApiController]
    [Route("api/forms")]
    public class FormSubmissionController : Controller
    {
        private readonly IFormVersionRepository _formVersions;
        private readonly IFormSubmissionService _submissionService;
        private readonly IEnumerable<IFormSubmitOutcomeType> _outcomeTypes;
        private readonly IFormSubmissionGuard? _formSubmissionGuard;
        private readonly ILogger<FormSubmissionController> _logger;

        public FormSubmissionController(
            IFormVersionRepository formVersions,
            IFormSubmissionService submissionService,
            IEnumerable<IFormSubmitOutcomeType> outcomeTypes,
            IFormSubmissionGuard? formSubmissionGuard,
            ILogger<FormSubmissionController> logger)
        {
            _formVersions = formVersions;
            _submissionService = submissionService;
            _outcomeTypes = outcomeTypes;
            _formSubmissionGuard = formSubmissionGuard;
            _logger = logger;
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

            values.TryGetValue("sf_PageUrl", out var pageUrl);

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
                    return RedirectBack(pageUrl);
                }
            }

            // File field values may only come from an actual upload, never from a posted text value
            foreach (var fileField in formVersion.Definition.Fields.Where(f => f.Configuration is FileFieldConfig))
            {
                values.Remove(fileField.Alias);
            }

            var request = new FormSubmissionRequest
            {
                Values = values.Where(it => formVersion.Definition.Fields.Any(f => f.Alias == it.Key)).ToDictionary(
                         kvp => kvp.Key,
                         kvp => JsonSerializer.SerializeToElement(kvp.Value)),
                PageUrl = pageUrl
            };

            var result = await _submissionService.SubmitAsync(formVersion, request, [.. Request.Form.Files]);

            if (!result.IsValid)
            {
                if (IsAjaxRequest(Request))
                {
                    return BadRequest(new AjaxFormResponse
                    {
                        Success = false,
                        Errors = result.Errors,
                        Values = values
                    });
                }

                TempData[$"{formVersion.FormId}:FormErrors"] = JsonSerializer.Serialize(result.Errors);
                TempData[$"{formVersion.FormId}:FormValues"] = JsonSerializer.Serialize(result.Values);
                return RedirectBack(pageUrl);
            }

            // The submission is saved at this point, so a broken outcome must not turn it into an error the visitor would resubmit
            var outcomeType = _outcomeTypes.FirstOrDefault(it => it.Alias == formVersion.Definition.SubmitOutcome.OutcomeTypeAlias);
            OutcomeResult? outcomeResult = null;
            if (outcomeType is null)
            {
                _logger.LogError("Form {FormId} uses submit outcome type {OutcomeTypeAlias}, which is not registered", formVersion.FormId, formVersion.Definition.SubmitOutcome.OutcomeTypeAlias);
            }
            else
            {
                outcomeResult = outcomeType.Handle(formVersion.Definition.SubmitOutcome.Configuration);
                outcomeResult.OutcomeTypeAlias = outcomeType.Alias;
            }

            if (IsAjaxRequest(Request))
            {
                return Ok(new AjaxFormResponse
                {
                    Success = true,
                    OutcomeType = outcomeResult?.OutcomeTypeAlias,
                    OutcomeData = outcomeResult?.Data
                });
            }

            if (outcomeResult is not null)
            {
                if (outcomeResult.Data.TryGetValue("url", out var redirectUrl) && redirectUrl is string url && !string.IsNullOrWhiteSpace(url))
                    return Redirect(url);
                else if (outcomeResult.Data.TryGetValue("message", out var message) && message is string msg && !string.IsNullOrWhiteSpace(msg))
                    TempData[$"{formVersion.FormId}:SuccessMessage"] = msg;
            }

            return RedirectBack(pageUrl);
        }

        /// <summary>
        /// Redirects to the page the form was posted from, but only to a page on this site; the posted page URL and the Referer header are both client-controlled.
        /// </summary>
        private RedirectResult RedirectBack(string? pageUrl)
        {
            return Redirect(
                SameHostUrl.GetOrNull(pageUrl, Request)
                ?? SameHostUrl.GetOrNull(Request.Headers.Referer.ToString(), Request)
                ?? "/");
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || request.Headers["Accept"].Any(x => x?.Contains("application/json") == true);
        }

        //TODO: Add endpoint for headless use
    }
}
