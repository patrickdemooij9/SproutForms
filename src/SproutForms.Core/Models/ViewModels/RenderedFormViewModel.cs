using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.ClientModels;
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

        // The variables forms.js works out as the visitor answers, for the conditions that use them, with their calculations
        public IReadOnlyList<FormClientVariable> Variables { get; init; } = [];
        public IReadOnlyList<CalculationRule> Calculations { get; init; } = [];
    }

}
