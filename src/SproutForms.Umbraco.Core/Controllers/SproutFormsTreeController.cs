using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace SproutForms.Umbraco.Core.Controllers
{
    [ApiExplorerSettings(GroupName = "Backoffice SproutForms")]
    [ApiController]
    [BackOfficeRoute("sproutForms")]
    [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
    [MapToApi("sproutForms")]
    public class SproutFormsTreeController : Controller
    {
        private readonly IFormRepository _formRepository;

        public SproutFormsTreeController(IFormRepository formRepository)
        {
            _formRepository = formRepository;
        }

        [HttpGet("root")]
        [ProducesResponseType(typeof(PagedViewModel<FormTreeItemModel>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedViewModel<FormTreeItemModel>>> GetRoot(int skip = 0, int take = 100)
        {
            var forms = _formRepository.Get(skip, take, out var total);
            var result = new PagedViewModel<FormTreeItemModel>
            {
                Items = forms.Select(it => new FormTreeItemModel
                {
                    Id = it.Id.ToString(),
                    Name = it.Name,
                }),
                Total = total
            };
            return Task.FromResult<ActionResult<PagedViewModel<FormTreeItemModel>>>(Ok(result));
        }

        [HttpGet("children")]
        [ProducesResponseType(typeof(PagedViewModel<FormTreeItemModel>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedViewModel<FormTreeItemModel>>> GetChildren(string parentUnique, int skip = 0, int take = 100)
        {
            return Ok(Enumerable.Empty<FormTreeItemModel>());
        }

        [HttpGet("ancestors")]
        [ProducesResponseType(typeof(IEnumerable<FormTreeItemModel>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<FormTreeItemModel>> GetAncestors(Guid descendantId)
        {
            return Ok(Enumerable.Empty<FormTreeItemModel>());
        }
    }
}
