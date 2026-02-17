using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Descriptors.Fields;
using SproutForms.Umbraco.Core.Descriptors.Flows;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using SproutForms.Umbraco.Core.Models.ViewModels;
using SproutForms.Umbraco.Core.Services;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Controllers
{
    [ApiExplorerSettings(GroupName = "Backoffice SproutForms")]
    [ApiController]
    [BackOfficeRoute("sproutForms")]
    [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
    [MapToApi("sproutForms")]
    public class SproutFormsBackofficeController : Controller
    {
        private readonly IFormFieldType[] _formFieldTypes;
        private readonly IFormSubmitOutcomeType[] _outcomeTypes;
        private readonly IFormWorkflowType[] _workflowTypes;
        private readonly IEnumerable<IFieldDescriptor> _fieldDescriptors;
        private readonly IEnumerable<IOutcomeDescriptor> _outcomeDescriptors;
        private readonly IEnumerable<IFlowDescriptor> _flowDescriptors;
        private readonly IFormFileStorageProvider _fileStorageProvider;
        private readonly IFormRepository _formRepository;
        private readonly IFormVersionRepository _formVersionRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
        private readonly ISproutFormsDashboardService _dashboardService;

        //TODO: Move each section (forms, submissions, flows) to their own controllers...
        public SproutFormsBackofficeController(IFormRepository formRepository, IFormVersionRepository formVersionRepository, IFormSubmissionRepository formSubmissionRepository, IEnumerable<IFieldDescriptor> fieldDescriptors, IEnumerable<IFormFieldType> formFieldTypes, IEnumerable<IOutcomeDescriptor> outcomeDescriptors, IEnumerable<IFormSubmitOutcomeType> outcomeTypes, IEnumerable<IFlowDescriptor> flowDescriptors, IEnumerable<IFormWorkflowType> workflowTypes, IFormFileStorageProvider fileStorageProvider, IBackOfficeSecurityAccessor backOfficeSecurityAccessor, IWorkflowExecutionRepository workflowExecutionRepository, ISproutFormsDashboardService dashboardService)
        {
            _formFieldTypes = formFieldTypes.ToArray();
            _outcomeTypes = outcomeTypes.ToArray();
            _workflowTypes = workflowTypes.ToArray();

            _formRepository = formRepository;
            _formVersionRepository = formVersionRepository;
            _formSubmissionRepository = formSubmissionRepository;
            _fieldDescriptors = fieldDescriptors;
            _outcomeDescriptors = outcomeDescriptors;
            _flowDescriptors = flowDescriptors;
            _fileStorageProvider = fileStorageProvider;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
            _workflowExecutionRepository = workflowExecutionRepository;
            _dashboardService = dashboardService;
        }

        [HttpGet("forms")]
        [ProducesResponseType(typeof(PagedViewModel<FormListBackofficeModel>), 200)]
        public IActionResult GetForms(int skip, int take)
        {
            var items = _formRepository.Get(skip, take, out var totalItems).Select(it => new FormListBackofficeModel
            {
                Id = it.Id,
                Name = it.Name,
                Source = (int)it.Source,
                TotalSubmissions = _formSubmissionRepository.Count(it.Id)
            }).ToArray();
            return Ok(new PagedViewModel<FormListBackofficeModel>()
            {
                Items = items,
                Total = totalItems
            });
        }

        [HttpGet("form")]
        [ProducesResponseType(typeof(FormBackofficeModel), 200)]
        public IActionResult GetForm(Guid id)
        {
            var form = _formRepository.GetById(id);
            if (form is null) return NotFound();

            var latestVersion = _formVersionRepository.GetLatest(id);
            if (latestVersion is null) return NotFound();

            var outcomeType = _outcomeTypes.First(it => it.Alias == latestVersion.Definition.SubmitOutcome.OutcomeTypeAlias);
            var outcomeDescriptor = _outcomeDescriptors.First(it => it.OutcomeTypeAlias == latestVersion.Definition.SubmitOutcome.OutcomeTypeAlias);
            return Ok(new FormBackofficeModel
            {
                Id = id,
                Name = form.Name,
                Version = latestVersion.Version,
                Alias = form.Alias,
                Source = (int)form.Source,
                Definition = new FormDefinitionBackofficeModel
                {
                    Outcome = new FormOutcomeBackofficeModel
                    {
                        TypeAlias = outcomeType.Alias,
                        DisplayName = outcomeDescriptor.DisplayName,
                        Configuration = outcomeDescriptor.FromConfig(latestVersion.Definition.SubmitOutcome.Configuration).ToDictionary(it => it.Alias, it => it.Value)
                    },
                    Rows = [.. latestVersion.Definition.Rows.Select(row => new FormRowBackofficeModel(row))],
                    Fields = [.. latestVersion.Definition.Fields.Select(field => Map(field))],
                    Workflows = latestVersion.Definition.Workflows.Select(flow =>
                    {
                        var flowType = _workflowTypes.First(it => it.Alias == flow.WorkflowTypeAlias);
                        var flowDescriptor = _flowDescriptors.First(f => f.FlowTypeAlias == flow.WorkflowTypeAlias);
                        return new FormWorkflowBackofficeModel
                        {
                            Alias = flow.Alias,
                            TypeAlias = flow.WorkflowTypeAlias,
                            DisplayName = flowDescriptor.DisplayName,
                            Order = flow.Order,
                            Configuration = flowDescriptor.FromConfig(flow.Configuration).ToDictionary(it => it.Alias, it => it.Value)
                        };
                    }).ToList()
                }
            });
        }

        [HttpPost("form")]
        [ProducesResponseType(typeof(FormBackofficeModel), 200)]
        public IActionResult SaveForm([FromBody]FormBackofficeModel model)
        {
            if (model.Id.HasValue)
            {
                var existingForm = _formRepository.GetById(model.Id.Value);
                if (existingForm != null && existingForm.Source == FormSource.Code)
                {
                    throw new InvalidOperationException("Can't edit form which is code based");
                }
            }

            var alias = model.Alias;
            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = GenerateAlias(model.Id, model.Name);
            }
            var duplicateFieldAliasses = GetDuplicateAliasses(model);
            if (duplicateFieldAliasses.Count > 0)
            {
                throw new InvalidOperationException($"Duplicated field aliasses: {string.Join(',', duplicateFieldAliasses)}");
            }

            var form = new Form
            {
                Id = model.Id ?? Guid.Empty,
                FolderId = model.FolderId,
                Name = model.Name,
                Alias = alias,
                Source = FormSource.UI
            };
            form.Id = _formRepository.Save(form);

            var latestVersion = _formVersionRepository.GetLatest(form.Id);
            var outcomeType = _outcomeTypes.First(it => it.Alias == model.Definition.Outcome.TypeAlias);
            var outcomeDescriptor = _outcomeDescriptors.First(it => it.OutcomeTypeAlias == model.Definition.Outcome.TypeAlias);
            var newDefinition = new FormDefinition
            {
                Rows = model.Definition.Rows.Select(r => new FormRow
                {
                    Columns = r.Columns.Select(c => new FormColumn
                    {
                        FieldAlias = c.FieldAlias,
                        Width = c.Width
                    }).ToList()
                }).ToList(),
                Fields = model.Definition.Fields.Select(Map).ToList(),
                Workflows = model.Definition.Workflows.Select(it =>
                {
                    var workflowType = _workflowTypes.First(w => w.Alias == it.TypeAlias);
                    var workflowDescriptor = _flowDescriptors.First(f => f.FlowTypeAlias == it.TypeAlias);
                    return new FormWorkflow
                    {
                        Alias = it.Alias,
                        WorkflowTypeAlias = it.TypeAlias,
                        Order = it.Order,
                        Configuration = workflowDescriptor.ToConfig(it.Configuration)
                    };
                }).ToList(),
                SubmitOutcome = new FormSubmitOutcome
                {
                    OutcomeTypeAlias = outcomeType.Alias,
                    Configuration = outcomeDescriptor.ToConfig(model.Definition.Outcome.Configuration)
                }
            };
            var hash = FormDefinitionHasher.Hash(newDefinition);
            _formVersionRepository.Add(new FormVersion
            {
                Id = Guid.NewGuid(),
                FormId = form.Id,
                Version = latestVersion?.Version + 1 ?? 1,
                Status = FormStatus.Published,
                Definition = newDefinition,
                DefinitionHash = hash,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key.ToString() ?? string.Empty
            });

            return GetForm(form.Id); //TODO: Probably just map everything back
        }

        [HttpGet("fieldTypes")]
        [ProducesResponseType(typeof(FormFieldTypeBackofficeModel[]), 200)]
        public IActionResult GetFieldTypes()
        {
            var formFieldTypes = new List<FormFieldTypeBackofficeModel>();
            foreach (var formFieldType in _formFieldTypes)
            {
                var descriptor = _fieldDescriptors.FirstOrDefault(it => it.FieldTypeAlias == formFieldType.Alias);
                if (descriptor is null) continue;

                formFieldTypes.Add(new FormFieldTypeBackofficeModel
                {
                    Alias = formFieldType.Alias,
                    DisplayName = descriptor.DisplayName,
                    Properties = descriptor.FromConfig(formFieldType.DefaultConfiguration)
                });
            }
            return Ok(formFieldTypes);
        }

        [HttpGet("outcomeTypes")]
        [ProducesResponseType(typeof(FormOutcomeTypeBackofficeModel[]), 200)]
        public IActionResult GetOutcomeTypes()
        {
            var outcomeTypes = new List<FormOutcomeTypeBackofficeModel>();
            foreach (var outcomeType in _outcomeTypes)
            {
                var descriptor = _outcomeDescriptors.FirstOrDefault(it => it.OutcomeTypeAlias == outcomeType.Alias);
                if (descriptor is null) continue;
                outcomeTypes.Add(new FormOutcomeTypeBackofficeModel
                {
                    Alias = outcomeType.Alias,
                    DisplayName = descriptor.DisplayName,
                    Properties = descriptor.FromConfig(outcomeType.GetDefaultConfiguration())
                });
            }
            return Ok(outcomeTypes);
        }

        [HttpGet("workflowTypes")]
        [ProducesResponseType(typeof(FormFlowTypeBackofficeModel[]), 200)]
        public IActionResult GetWorkflowTypes()
        {
            var workflowTypes = new List<FormFlowTypeBackofficeModel>();
            foreach (var flowType in _workflowTypes)
            {
                var descriptor = _flowDescriptors.FirstOrDefault(it => it.FlowTypeAlias == flowType.Alias);
                if (descriptor is null) continue;
                workflowTypes.Add(new FormFlowTypeBackofficeModel
                {
                    Alias = flowType.Alias,
                    DisplayName = descriptor.DisplayName,
                    DisplayTemplate = descriptor.DisplayTemplate,
                    Description = descriptor.Description,
                    Configuration = descriptor.FromConfig(flowType.GetDefaultConfiguration())
                });
            }
            return Ok(workflowTypes);
        }

        [HttpGet("submissions")]
        [ProducesResponseType(typeof(PagedViewModel<FormSubmissionListItemBackofficeModel>), 200)]
        public IActionResult GetSubmissions(Guid formId, int skip, int take)
        {
            var submissions = _formSubmissionRepository.GetByForm(formId, skip, take, out var totalCount);

            var submissionItems = submissions.Select(it =>
            {
                var workflowStages = new List<WorkflowStageStatusModel>();
                var formVersion = _formVersionRepository.Get(it.FormVersionId);

                if (formVersion != null)
                {
                    var workflowExecutions = _workflowExecutionRepository.GetBySubmissionId(it.Id).GetAwaiter().GetResult(); //TODO: Combine this together and do one database call for performance reasons...

                    foreach (var workflow in formVersion.Definition.Workflows)
                    {
                        var execution = workflowExecutions.FirstOrDefault(e => e.WorkflowAlias == workflow.Alias);
                        var flowDescriptor = _flowDescriptors.FirstOrDefault(f => f.FlowTypeAlias == workflow.WorkflowTypeAlias);

                        workflowStages.Add(new WorkflowStageStatusModel
                        {
                            WorkflowAlias = workflow.Alias,
                            DisplayName = flowDescriptor?.DisplayName ?? workflow.WorkflowTypeAlias,
                            Order = workflow.Order,
                            Status = execution?.Status ?? WorkflowExecutionStatus.Pending
                        });
                    }
                }

                return new FormSubmissionListItemBackofficeModel
                {
                    Id = it.Id,
                    Name = "Submission at " + it.SubmittedAt.ToString("G"),
                    WorkflowStages = workflowStages
                };
            }).ToArray();

            return Ok(new PagedViewModel<FormSubmissionListItemBackofficeModel>
            {
                Items = submissionItems,
                Total = totalCount
            });
        }

        [HttpGet("submission")]
        [ProducesResponseType(typeof(FormSubmissionBackofficeModel), 200)]
        public async Task<IActionResult> GetSubmission(Guid submissionId)
        {
            var submission = await _formSubmissionRepository.Get(submissionId);
            var formVersion = _formVersionRepository.Get(submission.FormVersionId);
            if (formVersion is null) return NotFound();

            var workflowStages = new List<WorkflowStageStatusModel>();
            var workflowExecutions = await _workflowExecutionRepository.GetBySubmissionId(submissionId);

            foreach (var workflow in formVersion.Definition.Workflows)
            {
                var execution = workflowExecutions.FirstOrDefault(e => e.WorkflowAlias == workflow.Alias);
                var flowDescriptor = _flowDescriptors.FirstOrDefault(f => f.FlowTypeAlias == workflow.WorkflowTypeAlias);

                workflowStages.Add(new WorkflowStageStatusModel
                {
                    WorkflowAlias = workflow.Alias,
                    DisplayName = flowDescriptor?.DisplayName ?? workflow.WorkflowTypeAlias,
                    Order = workflow.Order,
                    Status = execution?.Status ?? WorkflowExecutionStatus.Pending
                });
            }

            return Ok(new FormSubmissionBackofficeModel
            {
                Id = submission.Id,
                Values = submission.Values.Select(it =>
                {
                    var field = formVersion.Definition.Fields.FirstOrDefault(f => f.Alias == it.Key);
                    if (field is null) return null; //TODO: Fallback in place!!
                    var fieldType = _formFieldTypes.First(ft => ft.Alias == field.FieldTypeAlias);
                    return new FormSubmissionValueBackofficeModel
                    {
                        FieldTypeAlias = fieldType.Alias,
                        Name = field.Label,
                        Value = it.Value.ToString()
                    };
                }).WhereNotNull().ToArray(),
                WorkflowStages = workflowStages
            });
        }

        [HttpPost("submission/workflow/retry")]
        [ProducesResponseType(typeof(bool), 200)]
        public IActionResult RetryWorkflow(Guid submissionId, string workflowAlias)
        {
            // TODO: Regenerate SDK after implementing retry logic
            // This would involve:
            // 1. Getting the workflow execution for the given submission and workflow alias
            // 2. Resetting its status to Pending
            // 3. The workflow worker will pick it up again
            return Ok(true);
        }

        [HttpPost("submission/workflow/approve")]
        [ProducesResponseType(typeof(bool), 200)]
        public IActionResult ApproveWorkflow(Guid submissionId, string workflowAlias)
        {
            // TODO: Regenerate SDK after implementing manual approval logic
            // This would check if the workflow type supports manual approval
            // and then either skip the workflow execution or mark it as approved
            return Ok(true);
        }

        [HttpPost("submission/workflow/decline")]
        [ProducesResponseType(typeof(bool), 200)]
        public IActionResult DeclineWorkflow(Guid submissionId, string workflowAlias)
        {
            // TODO: Regenerate SDK after implementing manual decline logic
            // This would check if the workflow type supports manual approval
            // and then mark the workflow as failed with a decline message
            return Ok(true);
        }

        [HttpDelete("form")]
        public async Task<IActionResult> DeleteForm(Guid[] formIds)
        {
            foreach (var form in formIds)
            {
                _formSubmissionRepository.DeleteAllByForm(form);
                _formVersionRepository.DeleteAllByForm(form);
                _formRepository.Delete(form);
            }
            return Ok();
        }

        [HttpPost("generateAlias")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult GenerateAliasForForm(Guid? id, string name)
        {
            return Ok(GenerateAlias(id, name));
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(DashboardViewModel), 200)]
        public IActionResult GetDashboard()
        {
            return Ok(_dashboardService.GetDashboard());
        }

        /*[HttpGet("downloadFile")]
        public async Task<IActionResult> DownloadFile(StoredFileReference fileReference)
        {
            var stream = await _fileStorageProvider.OpenReadAsync(fileReference, CancellationToken.None);
            return File(stream, fileReference.ContentType);
        }*/

        private string GenerateAlias(Guid? id, string name)
        {
            var alias = name.ToLower().Replace(' ', '-');
            var existing = _formRepository.GetByAlias(alias);
            if (existing is null) return alias;
            if (id.HasValue && existing.Id == id.Value) return existing.Alias;

            var number = 1;
            while (true)
            {
                alias = $"{name.ToLower().Replace(' ', '-')}-{number}";
                existing = _formRepository.GetByAlias(alias);
                if (existing is null) return alias;
                if (id.HasValue && existing.Id == id.Value) return existing.Alias;
                number++;
            }
        }

        private List<string> GetDuplicateAliasses(FormBackofficeModel model) 
        {
            var duplicateAliasses = new List<string>();
            var aliasses = new List<string>();
            foreach (var field in model.Definition.Fields)
            {
                if (aliasses.Contains(field.Alias.ToLower()))
                {
                    duplicateAliasses.Add(field.Alias);
                }
                else
                {
                    aliasses.Add(field.Alias);
                }
            }
            return duplicateAliasses;
        }

        private FormFieldBackofficeModel Map(FormField field)
        {
            var result = new FormFieldBackofficeModel(field);
            var fieldType = _formFieldTypes.FirstOrDefault(ft => ft.Alias == field.FieldTypeAlias);
            var descriptor = _fieldDescriptors.First(ft => ft.FieldTypeAlias == fieldType.Alias);
            result.Configuration = descriptor.FromConfig(field.Configuration).ToDictionary(it => it.Alias, it => it.Value);
            result.Conditions = field.Conditions;
            return result;
        }

        private FormField Map(FormFieldBackofficeModel model)
        {
            var fieldType = _formFieldTypes.FirstOrDefault(ft => ft.Alias == model.FieldTypeAlias);
            var fieldDescritor = _fieldDescriptors.First(it => it.FieldTypeAlias == fieldType.Alias);
            var configuration = fieldDescritor.ToConfig(model.Configuration);
            var result = new FormField
            {
                Alias = model.Alias,
                Label = model.Label,
                FieldTypeAlias = model.FieldTypeAlias,
                Required = model.Required,
                Configuration = configuration,
                Conditions = model.Conditions
            };
            return result;
        }
    }
}
