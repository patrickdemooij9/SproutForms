using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Fields.Configs
{
    public class SelectFieldConfig
    {
        public IReadOnlyList<SelectFieldOption> Options { get; set; } = [];
    }
}
