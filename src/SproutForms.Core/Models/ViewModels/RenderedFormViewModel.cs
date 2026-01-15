using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class RenderedFormViewModel
    {
        public Guid Id { get; init; }
        public bool HasErrors { get; init; }

        public IReadOnlyList<FormRowViewModel> Rows { get; init; } = [];
    }

}
