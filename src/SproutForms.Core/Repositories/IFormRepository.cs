using SproutForms.Core.Models;

namespace SproutForms.Core.Repositories
{
    public interface IFormRepository
    {
        Form[] Get(int skip, int take, out int total);
        Form[] GetByFolder(Guid? folderId, int skip, int take, out int total);

        Form? GetByAlias(string alias);
        Form? GetById(Guid formId);
        Guid Save(Form form);
        void Delete(Guid formId);
    }
}
