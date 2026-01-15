using SproutForms.Core.Models;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormColumnBackofficeModel
    {
        public int Width { get; set; }
        public string FieldAlias { get; set; }

        public FormColumnBackofficeModel(FormColumn column)
        {
            Width = column.Width;
            FieldAlias = column.FieldAlias;
        }

        public FormColumnBackofficeModel()
        {
            
        }
    }
}
