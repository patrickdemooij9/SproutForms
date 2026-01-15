using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models
{
    public interface ICodeFirstForm
    {
        string Alias { get; }
        FormDefinition Build();
    }
}
