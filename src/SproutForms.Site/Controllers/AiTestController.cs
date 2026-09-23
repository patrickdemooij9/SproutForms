using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SproutForms.Core.Repositories;

namespace SproutForms.Site.Controllers
{
    /// <summary>
    /// Test harness for the AI verification flow (.claude/skills/verify-in-site). Only reachable in the AiTest environment.
    /// </summary>
    [Route("ai-test")]
    public class AiTestController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IFormRepository _formRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;

        public AiTestController(
            IWebHostEnvironment environment,
            IFormRepository formRepository,
            IFormSubmissionRepository formSubmissionRepository,
            IWorkflowExecutionRepository workflowExecutionRepository)
        {
            _environment = environment;
            _formRepository = formRepository;
            _formSubmissionRepository = formSubmissionRepository;
            _workflowExecutionRepository = workflowExecutionRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_environment.IsEnvironment("AiTest"))
                context.Result = NotFound();
        }

        [HttpGet("forms")]
        public IActionResult ListForms()
        {
            var forms = _formRepository.Get(0, int.MaxValue, out _);
            return Ok(forms.Select(it => new { it.Id, it.Alias, it.Name, Source = it.Source.ToString() }));
        }

        [HttpGet("forms/{alias}")]
        public IActionResult RenderForm(string alias)
        {
            if (_formRepository.GetByAlias(alias) is null)
                return NotFound($"No form with alias '{alias}'");

            return View("~/Views/AiTest/Form.cshtml", alias);
        }

        [HttpGet("forms/{alias}/submissions")]
        public async Task<IActionResult> GetSubmissions(string alias, int take = 10)
        {
            var form = _formRepository.GetByAlias(alias);
            if (form is null)
                return NotFound($"No form with alias '{alias}'");

            var submissions = _formSubmissionRepository.GetByForm(form.Id, 0, take, out var total);
            var items = new List<object>();
            foreach (var submission in submissions)
            {
                var executions = await _workflowExecutionRepository.GetBySubmissionId(submission.Id);
                items.Add(new
                {
                    submission.Id,
                    submission.FormVersionId,
                    submission.SubmittedAt,
                    submission.PageUrl,
                    submission.IpAddress,
                    submission.Values,
                    Workflows = executions.Select(e => new
                    {
                        e.WorkflowAlias,
                        e.WorkflowTypeAlias,
                        Status = e.Status.ToString(),
                        e.AttemptCount,
                        e.LastError,
                        e.StartedUtc,
                        e.CompletedUtc,
                        e.NextAttemptUtc
                    })
                });
            }

            return Ok(new { total, items });
        }
    }
}
