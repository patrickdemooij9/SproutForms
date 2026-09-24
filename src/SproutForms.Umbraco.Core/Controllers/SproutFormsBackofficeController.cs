using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SproutForms.Core.Helpers;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Repositories;
using SproutForms.Core.Services;
using SproutForms.Umbraco.Core.Descriptors.Fields;
using SproutForms.Umbraco.Core.Descriptors.Flows;
using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using SproutForms.Umbraco.Core.Models.ViewModels;
using SproutForms.Umbraco.Core.Services;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Extensions;
using WorkflowTemplate = SproutForms.Core.Models.WorkflowTemplate;

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
        private readonly IWorkflowTemplateRepository _templateRepository;
        private readonly IWorkflowRunner _workflowRunner;
        private readonly FormDeletionService _formDeletionService;
        private readonly FormDefinitionTypeValidator _formDefinitionTypeValidator;
        private readonly IFormDefinitionType[] _formDefinitionTypes;
        private readonly IEnumerable<IFormDefinitionTypeDescriptor> _formDefinitionTypeDescriptors;

        //TODO: Move each section (forms, submissions, flows) to their own controllers...
        public SproutFormsBackofficeController(IFormRepository formRepository, IFormVersionRepository formVersionRepository, IFormSubmissionRepository formSubmissionRepository, IEnumerable<IFieldDescriptor> fieldDescriptors, IEnumerable<IFormFieldType> formFieldTypes, IEnumerable<IOutcomeDescriptor> outcomeDescriptors, IEnumerable<IFormSubmitOutcomeType> outcomeTypes, IEnumerable<IFlowDescriptor> flowDescriptors, IEnumerable<IFormWorkflowType> workflowTypes, IFormFileStorageProvider fileStorageProvider, IBackOfficeSecurityAccessor backOfficeSecurityAccessor, IWorkflowExecutionRepository workflowExecutionRepository, ISproutFormsDashboardService dashboardService, IWorkflowTemplateRepository templateRepository, IWorkflowRunner workflowRunner, FormDeletionService formDeletionService, FormDefinitionTypeValidator formDefinitionTypeValidator, IEnumerable<IFormDefinitionType> formDefinitionTypes, IEnumerable<IFormDefinitionTypeDescriptor> formDefinitionTypeDescriptors)
        {
            _formDefinitionTypeValidator = formDefinitionTypeValidator;
            _formDefinitionTypes = formDefinitionTypes.ToArray();
            _formDefinitionTypeDescriptors = formDefinitionTypeDescriptors;
            _workflowRunner = workflowRunner;
            _formDeletionService = formDeletionService;
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
            _templateRepository = templateRepository;
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

            var formTypeDescriptor = GetViewableDescriptor(latestVersion.Definition.Type.TypeAlias);
            var outcomeAlias = latestVersion.Definition.SubmitOutcome.OutcomeTypeAlias;
            var outcomeType = GetRegistered(_outcomeTypes, it => it.Alias == outcomeAlias, "submit outcome", outcomeAlias);
            var outcomeDescriptor = GetRegistered(_outcomeDescriptors, it => it.OutcomeTypeAlias == outcomeAlias, "submit outcome", outcomeAlias);
            return Ok(new FormBackofficeModel
            {
                Id = id,
                Name = form.Name,
                Version = latestVersion.Version,
                Alias = form.Alias,
                Source = (int)form.Source,
                Definition = new FormDefinitionBackofficeModel
                {
                    Type = Map(latestVersion.Definition.Type),
                    Outcome = new FormOutcomeBackofficeModel
                    {
                        TypeAlias = outcomeType.Alias,
                        DisplayName = outcomeDescriptor.DisplayName,
                        Configuration = outcomeDescriptor.FromConfig(latestVersion.Definition.SubmitOutcome.Configuration).ToDictionary(it => it.Alias, it => it.Value)
                    },
                    Rows = [.. latestVersion.Definition.Rows.Select(row => new FormRowBackofficeModel(row))],
                    Fields = [.. latestVersion.Definition.Fields.Select(field => Map(field, formTypeDescriptor))],
                    Workflows = latestVersion.Definition.Workflows.Select(flow =>
                    {
                        var flowDescriptor = GetRegistered(_flowDescriptors, f => f.FlowTypeAlias == flow.WorkflowTypeAlias, "workflow type", flow.WorkflowTypeAlias);
                        return new FormWorkflowBackofficeModel
                        {
                            Alias = flow.Alias,
                            TypeAlias = flow.WorkflowTypeAlias,
                            DisplayName = flowDescriptor.DisplayName,
                            Order = flow.Order,
                            Configuration = flowDescriptor.FromConfig(flow.Configuration).ToDictionary(it => it.Alias, it => it.Value),
                            TemplateId = flow.TemplateId
                        };
                    }).ToList(),
                }
            });
        }

        [HttpPost("form")]
        [ProducesResponseType(typeof(FormBackofficeModel), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
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

            var latestVersion = model.Id.HasValue ? _formVersionRepository.GetLatest(model.Id.Value) : null;
            var formTypeAlias = model.Definition.Type.TypeAlias;
            var formType = GetRegistered(_formDefinitionTypes, it => it.Alias == formTypeAlias, "form type", formTypeAlias);
            var formTypeDescriptor = GetRegistered(_formDefinitionTypeDescriptors, it => it.FormTypeAlias == formTypeAlias, "form type", formTypeAlias);
            if (latestVersion != null && latestVersion.Definition.Type.TypeAlias != formTypeAlias)
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "Error",
                    Title = "The type of an existing form can't be changed."
                });
            }
            var outcomeType = GetRegistered(_outcomeTypes, it => it.Alias == model.Definition.Outcome.TypeAlias, "submit outcome", model.Definition.Outcome.TypeAlias);
            var outcomeDescriptor = GetRegistered(_outcomeDescriptors, it => it.OutcomeTypeAlias == model.Definition.Outcome.TypeAlias, "submit outcome", model.Definition.Outcome.TypeAlias);
            var newDefinition = new FormDefinition
            {
                Type = new FormDefinitionTypeReference
                {
                    TypeAlias = formType.Alias,
                    Settings = formTypeDescriptor.ToConfig(model.Definition.Type.Settings)
                },
                Rows = model.Definition.Rows.Select(r => new FormRow
                {
                    Columns = r.Columns.Select(c => new FormColumn
                    {
                        FieldAlias = c.FieldAlias,
                        Width = c.Width
                    }).ToList()
                }).ToList(),
                Fields = model.Definition.Fields.Select(field => Map(field, formType, formTypeDescriptor)).ToList(),
                Workflows = model.Definition.Workflows.Select(it =>
                {
                    GetRegistered(_workflowTypes, w => w.Alias == it.TypeAlias, "workflow type", it.TypeAlias);
                    var workflowDescriptor = GetRegistered(_flowDescriptors, f => f.FlowTypeAlias == it.TypeAlias, "workflow type", it.TypeAlias);
                    return new FormWorkflow
                    {
                        Alias = it.Alias,
                        WorkflowTypeAlias = it.TypeAlias,
                        Order = it.Order,
                        Configuration = workflowDescriptor.ToConfig(it.Configuration),
                        TemplateId = it.TemplateId
                    };
                }).ToList(),
                SubmitOutcome = new FormSubmitOutcome
                {
                    OutcomeTypeAlias = outcomeType.Alias,
                    Configuration = outcomeDescriptor.ToConfig(model.Definition.Outcome.Configuration)
                }
            };
            var typeErrors = _formDefinitionTypeValidator.Validate(newDefinition);
            if (typeErrors.Count > 0)
            {
                // The backoffice shows the title and these errors in its error notification
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["formType"] = [.. typeErrors]
                })
                {
                    // The backoffice only recognises problem details that have a type
                    Type = "Error",
                    Title = $"The form doesn't fit form type '{formTypeDescriptor.DisplayName}'."
                });
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
                    Icon = descriptor.Icon,
                    Properties = descriptor.FromConfig(formFieldType.DefaultConfiguration)
                });
            }
            return Ok(formFieldTypes);
        }

        [HttpGet("formTypes")]
        [ProducesResponseType(typeof(FormDefinitionTypeBackofficeModel[]), 200)]
        public IActionResult GetFormTypes()
        {
            var formTypes = new List<FormDefinitionTypeBackofficeModel>();
            foreach (var formType in _formDefinitionTypes)
            {
                var descriptor = _formDefinitionTypeDescriptors.FirstOrDefault(it => it.FormTypeAlias == formType.Alias);
                if (descriptor is null) continue;

                formTypes.Add(new FormDefinitionTypeBackofficeModel
                {
                    Alias = formType.Alias,
                    DisplayName = descriptor.DisplayName,
                    Description = descriptor.Description,
                    Properties = descriptor.FromConfig(formType.GetDefaultSettings()),
                    AllowedFieldTypeAliases = [.. _formFieldTypes.Where(it => FormDefinitionTypeValidator.IsAllowed(formType, it)).Select(it => it.Alias)],
                    AllowedOutcomeTypeAliases = [.. _outcomeTypes.Where(it => FormDefinitionTypeValidator.IsAllowed(formType, it)).Select(it => it.Alias)],
                    FieldExtensions = [.. formType.FieldExtensions.Select(extension =>
                    {
                        var extensionDescriptor = descriptor.FieldExtensions.FirstOrDefault(it => it.FieldTypeAlias == extension.FieldTypeAlias);
                        return extensionDescriptor is null ? null : new FormFieldExtensionBackofficeModel
                        {
                            FieldTypeAlias = extension.FieldTypeAlias,
                            Properties = extensionDescriptor.FromConfig(extension.GetDefaultSettings())
                        };
                    }).WhereNotNull()]
                });
            }
            return Ok(formTypes);
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

        [HttpGet("templates")]
        [ProducesResponseType(typeof(WorkflowTemplateBackofficeModel[]), 200)]
        public IActionResult GetTemplates()
        {
            var templates = _templateRepository.GetAll();
            return Ok(templates.Select(t => MapTemplate(t)).ToList());
        }

        [HttpGet("templates/{id}")]
        [ProducesResponseType(typeof(WorkflowTemplateBackofficeModel), 200)]
        public IActionResult GetTemplate(Guid id)
        {
            var template = _templateRepository.GetById(id);
            if (template is null) return NotFound();
            return Ok(MapTemplate(template));
        }

        [HttpPost("templates")]
        [ProducesResponseType(typeof(WorkflowTemplateBackofficeModel), 200)]
        public IActionResult CreateTemplate([FromBody] WorkflowTemplateBackofficeModel model)
        {
            var flowDescriptor = GetRegistered(_flowDescriptors, f => f.FlowTypeAlias == model.WorkflowTypeAlias, "workflow type", model.WorkflowTypeAlias);
            var template = new WorkflowTemplate
            {
                Id = model.Id ?? Guid.Empty,
                Name = model.Name,
                WorkflowTypeAlias = model.WorkflowTypeAlias,
                Configuration = flowDescriptor.ToConfig(model.Configuration),
                LockedFields = model.LockedFields.Select(flowDescriptor.ToPropertyName).WhereNotNull().ToList() ?? []
            };
            var savedId = _templateRepository.Save(template);
            template.Id = savedId;
            return Ok(MapTemplate(template));
        }

        [HttpPut("templates/{id}")]
        [ProducesResponseType(typeof(WorkflowTemplateBackofficeModel), 200)]
        public IActionResult UpdateTemplate(Guid id, [FromBody] WorkflowTemplateBackofficeModel model)
        {
            var flowDescriptor = GetRegistered(_flowDescriptors, f => f.FlowTypeAlias == model.WorkflowTypeAlias, "workflow type", model.WorkflowTypeAlias);
            var template = new WorkflowTemplate
            {
                Id = id,
                Name = model.Name,
                WorkflowTypeAlias = model.WorkflowTypeAlias,
                Configuration = flowDescriptor.ToConfig(model.Configuration),
                LockedFields = model.LockedFields.Select(flowDescriptor.ToPropertyName).WhereNotNull().ToList() ?? []
            };
            _templateRepository.Save(template);
            return Ok(MapTemplate(template));
        }

        [HttpDelete("templates/{id}")]
        [ProducesResponseType(204)]
        public IActionResult DeleteTemplate(Guid id)
        {
            _templateRepository.Delete(id);
            return NoContent();
        }

        private WorkflowTemplateBackofficeModel MapTemplate(WorkflowTemplate template)
        {
            var flowDescriptor = GetRegistered(_flowDescriptors, f => f.FlowTypeAlias == template.WorkflowTypeAlias, "workflow type", template.WorkflowTypeAlias);
            var configuration = flowDescriptor.FromConfig(template.Configuration);
            return new WorkflowTemplateBackofficeModel
            {
                Id = template.Id,
                Name = template.Name,
                WorkflowTypeAlias = template.WorkflowTypeAlias,
                Configuration = configuration.ToDictionary(it => it.Alias, it => it.Value),
                LockedFields = template.LockedFields.Select(it => configuration.FirstOrDefault(c => c.PropertyName.Equals(it))?.Alias).WhereNotNull().ToList()
            };
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
                    PageUrl = it.PageUrl,
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
                PageUrl = submission.PageUrl,
                Values = submission.Values.Select(it =>
                {
                    var field = formVersion.Definition.Fields.FirstOrDefault(f => f.Alias == it.Key);
                    if (field is null) return null; //TODO: Fallback in place!!
                    return new FormSubmissionValueBackofficeModel
                    {
                        FieldTypeAlias = field.FieldTypeAlias,
                        Name = field.Label,
                        Value = it.Value.ToString()
                    };
                }).WhereNotNull().ToArray(),
                WorkflowStages = workflowStages
            });
        }

        [HttpPost("submission/workflow/retry")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> RetryWorkflow(Guid submissionId, string workflowAlias)
        {
            return await _workflowRunner.RetryAsync(submissionId, workflowAlias) switch
            {
                WorkflowRetryResult.Queued => Ok(true),
                WorkflowRetryResult.NotFound => NotFound(),
                _ => Conflict("Only a failed or retrying workflow can be retried.")
            };
        }

        // Manual approval needs a design first: no workflow type supports it yet
        [HttpPost("submission/workflow/approve")]
        [ProducesResponseType(501)]
        public IActionResult ApproveWorkflow(Guid submissionId, string workflowAlias)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("submission/workflow/decline")]
        [ProducesResponseType(501)]
        public IActionResult DeclineWorkflow(Guid submissionId, string workflowAlias)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        [HttpDelete("form")]
        public async Task<IActionResult> DeleteForm(Guid[] formIds)
        {
            foreach (var form in formIds)
            {
                await _formDeletionService.DeleteAsync(form);
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

        // Posted form values are matched case-insensitively, so "Name" and "name" would collide
        private static List<string> GetDuplicateAliasses(FormBackofficeModel model)
        {
            var duplicateAliasses = new List<string>();
            var aliasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in model.Definition.Fields)
            {
                if (!aliasses.Add(field.Alias))
                {
                    duplicateAliasses.Add(field.Alias);
                }
            }
            return duplicateAliasses;
        }

        /// <summary>
        /// Finds the registered type or descriptor for an alias stored in a form, with an error that names the alias when it is no longer registered.
        /// </summary>
        private static T GetRegistered<T>(IEnumerable<T> items, Func<T, bool> match, string kind, string alias)
        {
            return items.FirstOrDefault(match)
                ?? throw new InvalidOperationException($"The form uses {kind} '{alias}', which is not registered. Register it again, or remove it from the form.");
        }

        // A code-first form can use a form type without a descriptor, or one that is no longer registered; it can still be viewed
        private IFormDefinitionTypeDescriptor? GetViewableDescriptor(string formTypeAlias)
        {
            var isRegistered = _formDefinitionTypes.Any(it => it.Alias == formTypeAlias);
            return isRegistered ? _formDefinitionTypeDescriptors.FirstOrDefault(it => it.FormTypeAlias == formTypeAlias) : null;
        }

        private FormTypeSelectionBackofficeModel Map(FormDefinitionTypeReference type)
        {
            var descriptor = GetViewableDescriptor(type.TypeAlias);
            return new FormTypeSelectionBackofficeModel
            {
                TypeAlias = type.TypeAlias,
                DisplayName = descriptor?.DisplayName ?? type.TypeAlias,
                Settings = descriptor?.FromConfig(type.Settings).ToDictionary(it => it.Alias, it => it.Value) ?? []
            };
        }

        private FormFieldBackofficeModel Map(FormField field, IFormDefinitionTypeDescriptor? formTypeDescriptor)
        {
            var result = new FormFieldBackofficeModel(field);
            var descriptor = GetRegistered(_fieldDescriptors, it => it.FieldTypeAlias == field.FieldTypeAlias, "field type", field.FieldTypeAlias);
            result.Configuration = descriptor.FromConfig(field.Configuration).ToDictionary(it => it.Alias, it => it.Value);
            result.Conditions = field.Conditions;

            // Settings the form type no longer describes stay as raw JSON, and are left out
            var extensionDescriptor = formTypeDescriptor?.FieldExtensions.FirstOrDefault(it => it.FieldTypeAlias == field.FieldTypeAlias);
            if (field.Extension != null && extensionDescriptor != null && field.Extension.Settings is not JsonElement)
            {
                result.Extension = extensionDescriptor.FromConfig(field.Extension.Settings).ToDictionary(it => it.Alias, it => it.Value);
            }
            return result;
        }

        private FormField Map(FormFieldBackofficeModel model, IFormDefinitionType formType, IFormDefinitionTypeDescriptor formTypeDescriptor)
        {
            GetRegistered(_formFieldTypes, it => it.Alias == model.FieldTypeAlias, "field type", model.FieldTypeAlias);
            var fieldDescritor = GetRegistered(_fieldDescriptors, it => it.FieldTypeAlias == model.FieldTypeAlias, "field type", model.FieldTypeAlias);
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

            // Every field of an extended field type gets the extension, with default settings when none were sent
            if (formType.FieldExtensions.Any(it => it.FieldTypeAlias == model.FieldTypeAlias))
            {
                var extensionDescriptor = GetRegistered(formTypeDescriptor.FieldExtensions, it => it.FieldTypeAlias == model.FieldTypeAlias, "field extension", $"{formType.Alias}/{model.FieldTypeAlias}");
                result.Extension = new FormFieldExtensionValue
                {
                    FormTypeAlias = formType.Alias,
                    Settings = extensionDescriptor.ToConfig(model.Extension ?? [])
                };
            }
            return result;
        }
    }
}
