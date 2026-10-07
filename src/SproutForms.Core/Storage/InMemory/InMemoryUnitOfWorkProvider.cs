using SproutForms.Core.Repositories;

namespace SproutForms.Core.Storage.InMemory
{
    /// <summary>
    /// The in-memory repositories write straight away, so a unit of work can't roll anything back.
    /// </summary>
    public class InMemoryUnitOfWorkProvider : IUnitOfWorkProvider
    {
        public IUnitOfWork Begin() => new UnitOfWork();

        private sealed class UnitOfWork : IUnitOfWork
        {
            public void Complete()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
