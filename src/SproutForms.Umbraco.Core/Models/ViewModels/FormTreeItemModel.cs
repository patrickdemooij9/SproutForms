using System.Text.Json.Serialization;
using Umbraco.Cms.Api.Management.ViewModels.Tree;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormTreeItemModel : TreeItemPresentationModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public TreeItemType ItemType { get; set; }
        public int? Source { get; set; }
        public int? TotalSubmissions { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TreeItemType
    {
        Folder = 0,
        Form = 1
    }
}
