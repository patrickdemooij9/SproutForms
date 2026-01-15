using Microsoft.AspNetCore.Http;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Files;

namespace SproutForms.Core.Services
{
    public interface IFormSubmissionService
    {
        Task<FormFileSubmitResult> HandleFileSubmits(FormVersion formVersion, IFormFile[] formFiles);
        Task<FormSubmissionResult> SubmitAsync(FormVersion formVersion, FormSubmissionRequest request);
    }
}
