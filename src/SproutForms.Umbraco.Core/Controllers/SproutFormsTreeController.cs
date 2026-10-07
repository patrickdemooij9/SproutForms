using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.ViewModels;
using SproutForms.Umbraco.Core.Repositories;
using SproutForms.Umbraco.Core.Security;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Web.Common.Routing;

namespace SproutForms.Umbraco.Core.Controllers
{
    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
    }

    [ApiExplorerSettings(GroupName = "Backoffice SproutForms")]
    [ApiController]
    [BackOfficeRoute("sproutForms")]
    [Authorize(Policy = SproutFormsAuthorization.TreeAccessPolicy)]
    [MapToApi("sproutForms")]
    public class SproutFormsTreeController : Controller
    {
        private readonly IFormRepository _formRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;

        public SproutFormsTreeController(IFormRepository formRepository,
            IFolderRepository folderRepository,
            IFormSubmissionRepository formSubmissionRepository)
        {
            _formRepository = formRepository;
            _folderRepository = folderRepository;
            _formSubmissionRepository = formSubmissionRepository;
        }

        [HttpGet("root")]
        [ProducesResponseType(typeof(PagedViewModel<FormTreeItemModel>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedViewModel<FormTreeItemModel>>> GetRoot(int skip = 0, int take = 100)
        {
            var folders = _folderRepository.GetRootFolders();
            var forms = _formRepository.GetByFolder(null, skip, take, out var total);

            var items = new List<FormTreeItemModel>();

            foreach (var folder in folders)
            {
                items.Add(new FormTreeItemModel
                {
                    Id = folder.Id.ToString(),
                    Name = folder.Name,
                    ItemType = TreeItemType.Folder
                });
            }

            foreach (var form in forms)
            {
                items.Add(new FormTreeItemModel
                {
                    Id = form.Id.ToString(),
                    Name = form.Name,
                    ItemType = TreeItemType.Form,
                    Source = (int)form.Source,
                    TotalSubmissions = _formSubmissionRepository.Count(form.Id)
                });
            }

            var result = new PagedViewModel<FormTreeItemModel>
            {
                Items = items,
                Total = folders.Length + total
            };
            return Task.FromResult<ActionResult<PagedViewModel<FormTreeItemModel>>>(Ok(result));
        }

        [HttpGet("children")]
        [ProducesResponseType(typeof(PagedViewModel<FormTreeItemModel>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedViewModel<FormTreeItemModel>>> GetChildren(string parentUnique, int skip = 0, int take = 100)
        {
            if (string.IsNullOrEmpty(parentUnique))
            {
                return GetRoot(skip, take);
            }

            if (!Guid.TryParse(parentUnique, out var parentId))
            {
                return Task.FromResult<ActionResult<PagedViewModel<FormTreeItemModel>>>(BadRequest("Invalid parent unique"));
            }

            var folders = _folderRepository.GetChildFolders(parentId);
            var forms = _formRepository.GetByFolder(parentId, skip, take, out var total);

            var items = new List<FormTreeItemModel>();

            foreach (var folder in folders)
            {
                items.Add(new FormTreeItemModel
                {
                    Id = folder.Id.ToString(),
                    Name = folder.Name,
                    ItemType = TreeItemType.Folder
                });
            }

            foreach (var form in forms)
            {
                items.Add(new FormTreeItemModel
                {
                    Id = form.Id.ToString(),
                    Name = form.Name,
                    ItemType = TreeItemType.Form,
                    Source = (int)form.Source,
                    TotalSubmissions = _formSubmissionRepository.Count(form.Id)
                });
            }

            var result = new PagedViewModel<FormTreeItemModel>
            {
                Items = items,
                Total = folders.Length + total
            };
            return Task.FromResult<ActionResult<PagedViewModel<FormTreeItemModel>>>(Ok(result));
        }

        /// <summary>
        /// A form as the tree shows it, so the form picker can show the name of the picked form
        /// </summary>
        [HttpGet("item")]
        [ProducesResponseType(typeof(FormTreeItemModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<FormTreeItemModel> GetItem(Guid id)
        {
            var form = _formRepository.GetById(id);
            if (form is null)
            {
                return NotFound();
            }

            return Ok(new FormTreeItemModel
            {
                Id = form.Id.ToString(),
                Name = form.Name,
                ItemType = TreeItemType.Form,
                Source = (int)form.Source
            });
        }

        [HttpGet("ancestors")]
        [ProducesResponseType(typeof(IEnumerable<FormTreeItemModel>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<FormTreeItemModel>> GetAncestors(Guid descendantId)
        {
            var form = _formRepository.GetById(descendantId);
            if (form is null)
            {
                return NotFound();
            }

            var ancestors = new List<FormTreeItemModel>();
            var currentFolderId = form.FolderId;

            while (currentFolderId.HasValue)
            {
                var folder = _folderRepository.GetById(currentFolderId.Value);
                if (folder is null)
                {
                    break;
                }

                ancestors.Insert(0, new FormTreeItemModel
                {
                    Id = folder.Id.ToString(),
                    Name = folder.Name,
                    ItemType = TreeItemType.Folder
                });

                currentFolderId = folder.ParentId;
            }

            return Ok(ancestors);
        }

        [HttpPost("folder")]
        [Authorize(Policy = SproutFormsAuthorization.SectionAccessPolicy)]
        [ProducesResponseType(typeof(FormTreeItemModel), StatusCodes.Status200OK)]
        public ActionResult<FormTreeItemModel> CreateFolder([FromBody] CreateFolderRequest request)
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                ParentId = request.ParentId.HasValue && request.ParentId.Value != Guid.Empty ? request.ParentId : null,
                SortOrder = 0
            };

            _folderRepository.Save(folder);

            return Ok(new FormTreeItemModel
            {
                Id = folder.Id.ToString(),
                Name = folder.Name,
                ItemType = TreeItemType.Folder
            });
        }
    }
}
