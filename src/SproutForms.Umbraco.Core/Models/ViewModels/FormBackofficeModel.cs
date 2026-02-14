using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormBackofficeModel
    {
        public Guid? Id { get; set; }
        public Guid? FolderId { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public int Version { get; set; }
        public int Source { get; set; }
        public FormDefinitionBackofficeModel Definition { get; set; }
    }
}
