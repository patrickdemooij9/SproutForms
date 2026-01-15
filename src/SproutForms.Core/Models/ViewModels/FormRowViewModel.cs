using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormRowViewModel
    {
        public IReadOnlyList<FormColumnViewModel> Columns { get; init; } = [];
    }
}
