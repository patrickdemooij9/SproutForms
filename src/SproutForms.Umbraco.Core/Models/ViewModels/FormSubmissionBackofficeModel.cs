using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormSubmissionBackofficeModel
    {
        public Guid Id { get; set; }
        public FormSubmissionValueBackofficeModel[] Values { get; set; } = [];
    }
}
