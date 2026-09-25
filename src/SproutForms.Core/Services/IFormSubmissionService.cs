using Microsoft.AspNetCore.Http;
using SproutForms.Core.Models;
using System.Text.Json;

namespace SproutForms.Core.Services
{
    public interface IFormSubmissionService
    {
        /// <summary>
        /// Stores the uploaded files, validates the submission, lets the form's type process it and saves it with its results and workflow queue. Uploaded files are deleted again when the submission is rejected or fails.
        /// </summary>
        Task<FormSubmissionResult> SubmitAsync(FormVersion formVersion, FormSubmissionRequest request, IReadOnlyList<IFormFile> files);

        /// <summary>
        /// Validates the fields of one page against the values entered so far, without saving anything. Upload fields are left to the final submit, which receives the files.
        /// </summary>
        IReadOnlyDictionary<string, List<string>> ValidatePage(FormVersion formVersion, int pageIndex, IReadOnlyDictionary<string, JsonElement> values);
    }
}
