using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public sealed class FormColumnViewModel
    {
        public int Width { get; init; } // 1–12
        public FormFieldViewModel Field { get; init; } = default!;
    }
}
