using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormRowBackofficeModel
    {
        public List<FormColumnBackofficeModel> Columns { get; set; } = new();

        public FormRowBackofficeModel(FormRow row)
        {
            Columns = [.. row.Columns.Select(it => new FormColumnBackofficeModel(it))];
        }

        public FormRowBackofficeModel()
        {
            
        }
    }
}
