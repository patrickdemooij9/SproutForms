using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class WorkflowTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string WorkflowTypeAlias { get; set; } = string.Empty;
        public required object Configuration { get; set; }
        public List<string> LockedFields { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
