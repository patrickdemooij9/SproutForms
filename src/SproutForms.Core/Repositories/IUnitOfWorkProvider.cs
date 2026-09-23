namespace SproutForms.Core.Repositories
{
    public interface IUnitOfWorkProvider
    {
        /// <summary>
        /// Starts a unit of work that repository calls made inside it take part in. Nothing is committed unless <see cref="IUnitOfWork.Complete"/> is called before it is disposed.
        /// </summary>
        IUnitOfWork Begin();
    }

    public interface IUnitOfWork : IDisposable
    {
        void Complete();
    }
}
