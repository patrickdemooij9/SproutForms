namespace SproutForms.Core.Models
{
    // Stored as numbers, so existing values must keep their number
    public enum FormAuditAction
    {
        Created = 0,
        Saved = 1,
        Renamed = 2,
        Moved = 3,
        RolledBack = 4
    }
}
