using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Umbraco.Core.Models.ViewModels;
using SproutForms.Umbraco.Core.Security;
using SproutForms.Umbraco.Core.Services;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Routing;

namespace SproutForms.Umbraco.Core.Controllers
{
    /// <summary>
    /// Deleting a form or a submission in the backoffice moves it to the recycle bin; it is only deleted for good from there.
    /// Every form has a recycle bin of its own for its submissions.
    /// </summary>
    [ApiExplorerSettings(GroupName = "Backoffice SproutForms")]
    [ApiController]
    [BackOfficeRoute("sproutForms")]
    [Authorize(Policy = SproutFormsAuthorization.SectionAccessPolicy)]
    [MapToApi("sproutForms")]
    public class SproutFormsRecycleBinController : Controller
    {
        private readonly FormRecycleBinService _formRecycleBinService;
        private readonly FormSubmissionRecycleBinService _submissionRecycleBinService;
        private readonly IFormRepository _formRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;
        private readonly BackofficeUserNameResolver _userNameResolver;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        public SproutFormsRecycleBinController(
            FormRecycleBinService formRecycleBinService,
            FormSubmissionRecycleBinService submissionRecycleBinService,
            IFormRepository formRepository,
            IFolderRepository folderRepository,
            IFormSubmissionRepository formSubmissionRepository,
            BackofficeUserNameResolver userNameResolver,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        {
            _formRecycleBinService = formRecycleBinService;
            _submissionRecycleBinService = submissionRecycleBinService;
            _formRepository = formRepository;
            _folderRepository = folderRepository;
            _formSubmissionRepository = formSubmissionRepository;
            _userNameResolver = userNameResolver;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        [HttpPost("form/trash")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public IActionResult MoveToRecycleBin([FromBody] Guid[] formIds)
        {
            var errors = _formRecycleBinService.MoveToRecycleBin(formIds, GetCurrentUserKey());
            if (errors.Count > 0)
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["forms"] = [.. errors]
                })
                {
                    // The backoffice only recognises problem details that have a type
                    Type = "Error",
                    Title = "Nothing was moved to the recycle bin."
                });
            }
            return Ok();
        }

        [HttpGet("recycleBin")]
        [ProducesResponseType(typeof(PagedViewModel<TrashedFormBackofficeModel>), 200)]
        public async Task<IActionResult> GetRecycleBin(int skip = 0, int take = 100)
        {
            var forms = _formRepository.GetTrashed(skip, take, out var total);
            var userNames = new Dictionary<string, string>();

            var items = new List<TrashedFormBackofficeModel>();
            foreach (var form in forms)
            {
                items.Add(new TrashedFormBackofficeModel
                {
                    Id = form.Id,
                    Name = form.Name,
                    Alias = form.Alias,
                    TrashedAt = form.TrashedAt!.Value,
                    TrashedByName = await _userNameResolver.GetNameAsync(form.TrashedBy ?? string.Empty, userNames),
                    FolderName = form.FolderId is { } folderId ? _folderRepository.GetById(folderId)?.Name : null,
                    TotalSubmissions = _formSubmissionRepository.Count(form.Id)
                });
            }

            return Ok(new PagedViewModel<TrashedFormBackofficeModel>
            {
                Items = items,
                Total = total
            });
        }

        [HttpPost("recycleBin/restore")]
        [ProducesResponseType(200)]
        public IActionResult Restore([FromBody] Guid[] formIds)
        {
            _formRecycleBinService.Restore(formIds, GetCurrentUserKey());
            return Ok();
        }

        [HttpDelete("recycleBin")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeletePermanently([FromBody] Guid[] formIds)
        {
            await _formRecycleBinService.DeletePermanentlyAsync(formIds);
            return Ok();
        }

        [HttpDelete("recycleBin/all")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Empty()
        {
            await _formRecycleBinService.EmptyAsync();
            return Ok();
        }

        [HttpPost("submissions/trash")]
        [ProducesResponseType(200)]
        public IActionResult MoveSubmissionsToRecycleBin(Guid formId, [FromBody] Guid[] submissionIds)
        {
            _submissionRecycleBinService.MoveToRecycleBin(formId, submissionIds, GetCurrentUserKey());
            return Ok();
        }

        [HttpPost("submissions/recycleBin/restore")]
        [ProducesResponseType(200)]
        public IActionResult RestoreSubmissions(Guid formId, [FromBody] Guid[] submissionIds)
        {
            _submissionRecycleBinService.Restore(formId, submissionIds, GetCurrentUserKey());
            return Ok();
        }

        [HttpDelete("submissions/recycleBin")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteSubmissionsPermanently(Guid formId, [FromBody] Guid[] submissionIds)
        {
            await _submissionRecycleBinService.DeletePermanentlyAsync(formId, submissionIds, GetCurrentUserKey());
            return Ok();
        }

        [HttpDelete("submissions/recycleBin/all")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> EmptySubmissionsRecycleBin(Guid formId)
        {
            await _submissionRecycleBinService.EmptyAsync(formId, GetCurrentUserKey());
            return Ok();
        }

        private string GetCurrentUserKey()
            => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key.ToString() ?? string.Empty;
    }
}
