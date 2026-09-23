using Microsoft.AspNetCore.Http;
using SproutForms.Core.Models;

namespace SproutForms.Core.Services
{
    public interface IFormSubmissionService
    {
        /// <summary>
        /// Stores the uploaded files, validates the submission and saves it with its workflow queue. Uploaded files are deleted again when the submission is rejected or fails.
        /// </summary>
        Task<FormSubmissionResult> SubmitAsync(FormVersion formVersion, FormSubmissionRequest request, IReadOnlyList<IFormFile> files);
    }
}
