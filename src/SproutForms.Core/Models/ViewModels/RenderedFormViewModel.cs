using SproutForms.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class RenderedFormViewModel
    {
        public Guid Id { get; init; }
        public bool HasErrors { get; init; }

        public IReadOnlyList<FormPageViewModel> Pages { get; init; } = [];
        public required string SubmitLabel { get; init; }
        public bool ShowProgress { get; init; }
        public IReadOnlyList<FormSubmissionGuardViewModel> SubmissionGuards { get; init; } = [];
    }

}
