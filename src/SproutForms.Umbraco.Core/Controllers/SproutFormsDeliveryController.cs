using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.ClientModels;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Umbraco.Core.Headless;
using SproutForms.Umbraco.Core.Models.Headless;
using System.Text.Json;
using Umbraco.Cms.Api.Common.Attributes;

namespace SproutForms.Umbraco.Core.Controllers
{
    /// <summary>
    /// The headless API: a published form's definition, and submitting it, for front-ends that don't render forms with Razor.
    /// Off unless SproutForms:Headless:Enabled is set. There is no antiforgery token, since the front-end runs on another origin;
    /// the submission guard is what keeps bots out.
    /// </summary>
    [ApiController]
    [Route(HeadlessApi.RoutePrefix)]
    [MapToApi(HeadlessApi.DocumentName)]
    [ApiExplorerSettings(GroupName = "Forms")]
    [EnableCors(HeadlessApi.CorsPolicyName)]
    [HeadlessApiAccess]
    public class SproutFormsDeliveryController : ControllerBase
    {
        private static readonly JsonSerializerOptions MultipartJsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IFormRepository _forms;
        private readonly IFormVersionRepository _formVersions;
        private readonly FormClientModelBuilder _clientModelBuilder;
        private readonly IFormSubmissionService _submissionService;
        private readonly FormSubmitOutcomeRunner _outcomeRunner;
        private readonly IFormSubmissionGuard? _formSubmissionGuard;

        public SproutFormsDeliveryController(
            IFormRepository forms,
            IFormVersionRepository formVersions,
            FormClientModelBuilder clientModelBuilder,
            IFormSubmissionService submissionService,
            FormSubmitOutcomeRunner outcomeRunner,
            IFormSubmissionGuard? formSubmissionGuard)
        {
            _forms = forms;
            _formVersions = formVersions;
            _clientModelBuilder = clientModelBuilder;
            _submissionService = submissionService;
            _outcomeRunner = outcomeRunner;
            _formSubmissionGuard = formSubmissionGuard;
        }

        /// <summary>
        /// The published version of a form, by id or alias. The ETag is the version, so a client can revalidate its copy with If-None-Match.
        /// </summary>
        [HttpGet("definitions/{idOrAlias}")]
        [ProducesResponseType<FormClientModel>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<FormClientModel> GetDefinition(string idOrAlias)
        {
            var form = Guid.TryParse(idOrAlias, out var id) ? _forms.GetById(id) : _forms.GetByAlias(idOrAlias);
            var formVersion = GetPublishedVersion(form);
            if (form is null || formVersion is null)
                return NotFound();

            var etag = $"\"{formVersion.Id:N}\"";
            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "no-cache";
            if (Request.Headers.IfNoneMatch.Contains(etag))
                return StatusCode(StatusCodes.Status304NotModified);

            return _clientModelBuilder.Build(form, formVersion);
        }

        /// <summary>
        /// Submits a form: as application/json, or as multipart/form-data when it has uploads (see <see cref="HeadlessSubmitRequest"/>).
        /// A rejected submission is a 400 with the errors per field alias; "submissionGuard" and aliases that aren't fields are about the whole form.
        /// </summary>
        [HttpPost("entries/{id:guid}")]
        [HeadlessSubmitRequestBody]
        [ProducesResponseType<HeadlessSubmitResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> Submit(Guid id)
        {
            var formVersion = GetPublishedVersion(_forms.GetById(id));
            if (formVersion is null)
                return NotFound();

            // Not a [Consumes] constraint: that also rejects the CORS preflight, which has no content type
            if (!Request.HasFormContentType && !Request.HasJsonContentType())
                return StatusCode(StatusCodes.Status415UnsupportedMediaType);

            var (request, files) = await ReadSubmitRequestAsync();
            if (request is null)
                return Invalid(new Dictionary<string, List<string>> { ["body"] = ["The request body is not valid."] });

            var values = SubmittedFieldValues.Filter(formVersion, ToSubmittedValues(request.Values));

            if (_formSubmissionGuard is not null)
            {
                // A guard reads what a posted HTML form would hold: every value as text, with its own fields
                var postedValues = values
                    .Where(it => it.Value.ValueKind == JsonValueKind.String)
                    .ToDictionary(it => it.Key, it => it.Value.GetString()!);
                foreach (var (key, value) in request.Guard)
                    postedValues[key] = value;

                var guardResult = await _formSubmissionGuard.EvaluateAsync(postedValues);
                if (!guardResult.Allowed)
                    return Invalid(new Dictionary<string, List<string>> { ["submissionGuard"] = [guardResult.ErrorMessage!] });
            }

            var result = await _submissionService.SubmitAsync(formVersion, new FormSubmissionRequest
            {
                Values = values,
                PageUrl = request.PageUrl
            }, files);

            if (!result.IsValid)
                return Invalid(result.Errors);

            var outcome = await _outcomeRunner.RunAsync(formVersion, result.Submission!, HttpContext.RequestAborted);
            return Ok(new HeadlessSubmitResponse
            {
                Outcome = outcome is null ? null : new HeadlessOutcome
                {
                    Type = outcome.OutcomeTypeAlias,
                    Data = outcome.Data
                }
            });
        }

        /// <summary>
        /// Validates one page of a form before the visitor moves on to the next, so rules only the server checks show on the page they
        /// belong to. Nothing is saved, and uploads are left to the final submit.
        /// </summary>
        [HttpPost("entries/{id:guid}/pages/{pageIndex:int}/validate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ValidatePage(Guid id, int pageIndex, [FromBody] HeadlessValidatePageRequest request)
        {
            var formVersion = GetPublishedVersion(_forms.GetById(id));
            if (formVersion is null || pageIndex < 0 || pageIndex >= formVersion.Definition.Pages.Count)
                return NotFound();

            var errors = _submissionService.ValidatePage(formVersion, pageIndex, SubmittedFieldValues.Filter(formVersion, ToSubmittedValues(request.Values)));
            return errors.Count == 0 ? NoContent() : Invalid(errors);
        }

        // A form in the recycle bin is treated as deleted
        private FormVersion? GetPublishedVersion(Form? form)
            => form is { IsTrashed: false } ? _formVersions.GetPublished(form.Id) : null;

        private async Task<(HeadlessSubmitRequest? Request, IReadOnlyList<IFormFile> Files)> ReadSubmitRequestAsync()
        {
            try
            {
                if (Request.HasFormContentType)
                {
                    var form = await Request.ReadFormAsync(HttpContext.RequestAborted);
                    return (new HeadlessSubmitRequest
                    {
                        Values = ReadJsonPart<Dictionary<string, JsonElement>>(form, "values") ?? [],
                        PageUrl = form["pageUrl"].ToString(),
                        Guard = ReadJsonPart<Dictionary<string, string>>(form, "guard") ?? []
                    }, [.. form.Files]);
                }

                var request = await JsonSerializer.DeserializeAsync<HeadlessSubmitRequest>(Request.Body, MultipartJsonOptions, HttpContext.RequestAborted);
                return (request, []);
            }
            catch (JsonException)
            {
                return (null, []);
            }
        }

        private static T? ReadJsonPart<T>(IFormCollection form, string name)
        {
            var raw = form[name].ToString();
            return string.IsNullOrWhiteSpace(raw) ? default : JsonSerializer.Deserialize<T>(raw, MultipartJsonOptions);
        }

        // Validation works on text, as a posted HTML form sends it, so JSON numbers and booleans become their text; lists and objects stay as they are
        private static IEnumerable<KeyValuePair<string, JsonElement>> ToSubmittedValues(Dictionary<string, JsonElement> values)
        {
            foreach (var (alias, value) in values)
            {
                var submitted = value.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => (JsonElement?)null,
                    JsonValueKind.True => JsonSerializer.SerializeToElement("true"),
                    JsonValueKind.False => JsonSerializer.SerializeToElement("false"),
                    JsonValueKind.Number => JsonSerializer.SerializeToElement(value.GetRawText()),
                    _ => value
                };
                if (submitted.HasValue)
                    yield return KeyValuePair.Create(alias, submitted.Value);
            }
        }

        private ObjectResult Invalid(IReadOnlyDictionary<string, List<string>> errors)
            => BadRequest(new ValidationProblemDetails(errors.ToDictionary(it => it.Key, it => it.Value.ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The submission is not valid."
            });
    }
}
