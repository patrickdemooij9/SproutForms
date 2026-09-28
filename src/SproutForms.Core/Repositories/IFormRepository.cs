using SproutForms.Core.Models;

namespace SproutForms.Core.Repositories
{
    public interface IFormRepository
    {
        // Leave out the forms in the recycle bin
        Form[] Get(int skip, int take, out int total);
        Form[] GetByFolder(Guid? folderId, int skip, int take, out int total);

        // Most recently trashed first
        Form[] GetTrashed(int skip, int take, out int total);

        // Also return a form in the recycle bin: check Form.IsTrashed before showing it to visitors
        Form? GetByAlias(string alias);
        Form? GetById(Guid formId);

        Guid Save(Form form);
        void MoveToRecycleBin(Guid formId, DateTime trashedAt, string trashedBy);
        void RestoreFromRecycleBin(Guid formId, Guid? folderId);
        void Delete(Guid formId);
    }
}
