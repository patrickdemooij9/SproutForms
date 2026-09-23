using SproutForms.Core.Repositories;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SproutForms.Umbraco.Core.Repositories
{
    /// <summary>
    /// Wraps an Umbraco scope; the repositories' own scopes nest inside it and commit or roll back with it.
    /// </summary>
    public class ScopeUnitOfWorkProvider : IUnitOfWorkProvider
    {
        private readonly IScopeProvider _scopeProvider;

        public ScopeUnitOfWorkProvider(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public IUnitOfWork Begin()
        {
            return new ScopeUnitOfWork(_scopeProvider.CreateScope());
        }

        private sealed class ScopeUnitOfWork : IUnitOfWork
        {
            private readonly IScope _scope;

            public ScopeUnitOfWork(IScope scope)
            {
                _scope = scope;
            }

            public void Complete() => _scope.Complete();

            public void Dispose() => _scope.Dispose();
        }
    }
}
