using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IFormRepository _forms;
        private readonly IFormVersionRepository _formVersions;
        private readonly IFormSubmissionService _submissionService;
        private readonly FormSubmitOutcomeRunner _outcomeRunner;
        private readonly IFormSubmissionGuard? _formSubmissionGuard;

        public FormSubmissionController(
            IFormRepository forms,
            IFormVersionRepository formVersions,
            IFormSubmissionService submissionService,
            FormSubmitOutcomeRunner outcomeRunner,
            IFormSubmissionGuard? formSubmissionGuard)
        {
            _forms = forms;
            _formVersions = formVersions;
            _submissionService = submissionService;
            _outcomeRunner = outcomeRunner;
            _formSubmissionGuard = formSubmissionGuard;
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid id)
        {
            var values = GetPostedValues();
            var formVersion = GetPublishedVersion(id);
            if (formVersion is null)
                return NotFound();

            values.TryGetValue(FormSubmissionRequest.PageUrlKey, out var pageUrl);

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

            var request = new FormSubmissionRequest
            {
                Values = GetFieldValues(values),
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

            var outcomeResult = await _outcomeRunner.RunAsync(formVersion, result.Submission!, HttpContext.RequestAborted);

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
        /// Validates one page of a form before the visitor moves on to the next, so rules only the server checks show on the page they belong to. Nothing is saved.
        /// </summary>
        [HttpPost("{id}/pages/{pageIndex:int}/validate")]
        [ValidateAntiForgeryToken]
        public IActionResult ValidatePage(Guid id, int pageIndex)
        {
            var values = GetPostedValues();
            var formVersion = GetPublishedVersion(id);
            if (formVersion is null || pageIndex < 0 || pageIndex >= formVersion.Definition.Pages.Count)
                return NotFound();

            var errors = _submissionService.ValidatePage(formVersion, pageIndex, GetFieldValues(values));
            if (errors.Count != 0)
            {
                return BadRequest(new AjaxFormResponse
                {
                    Success = false,
                    Errors = errors
                });
            }

            return Ok(new AjaxFormResponse { Success = true });
        }

        // A form in the recycle bin takes no submissions, as if it was deleted
        private FormVersion? GetPublishedVersion(Guid formId)
            => _forms.GetById(formId) is { IsTrashed: false } ? _formVersions.GetPublished(formId) : null;

        // Read from the form itself: model binding would take the names of a repeater's inputs, such as "people[0].firstName", for its own
        // dictionary syntax. A checkbox posts its value before the hidden "false" after it, so the first value is the one that counts
        private Dictionary<string, string> GetPostedValues()
            => Request.Form.ToDictionary(it => it.Key, it => it.Value.FirstOrDefault() ?? string.Empty);

        // The inputs of a repeater's entries are named by their path, such as "people[0].firstName"; the submission service keeps the form's own fields
        private static Dictionary<string, JsonElement> GetFieldValues(Dictionary<string, string> values)
            => SubmittedFieldValues.FromPostedForm(values);

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
    }
}
