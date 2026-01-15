using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormFieldTypeBackofficeModel
    {
        public Guid Id { get; set; }
        public string Alias { get; set; }
        public string DisplayName { get; set; }
        public string Icon { get; set; }
        public FormPropertyBackofficeModel[] Properties { get; set; }
    }
}
