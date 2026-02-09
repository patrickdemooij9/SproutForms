namespace SproutForms.Core.Fields.Configs
{
    public class DateFieldConfig
    {
        public DateTime? Min { get; set; }
        public DateTime? Max { get; set; }
        public bool IncludeTime { get; set; } = false;
    }
}
