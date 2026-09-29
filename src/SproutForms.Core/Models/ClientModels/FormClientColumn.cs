namespace SproutForms.Core.Models.ClientModels
{
    public sealed class FormClientColumn
    {
        public int Width { get; init; } // 1–12
        public required FormClientField Field { get; init; }
    }
}
