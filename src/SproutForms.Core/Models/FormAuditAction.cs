namespace SproutForms.Core.Models
{
    // Stored as numbers, so existing values must keep their number
    public enum FormAuditAction
    {
        Created = 0,
        Saved = 1,
        Renamed = 2,
        Moved = 3,
        RolledBack = 4,
        MovedToRecycleBin = 5,
        RestoredFromRecycleBin = 6,
        // The entries of a form, one entry per action however many submissions it covered
        SubmissionsMovedToRecycleBin = 7,
        SubmissionsRestoredFromRecycleBin = 8,
        SubmissionsDeleted = 9
    }
}
