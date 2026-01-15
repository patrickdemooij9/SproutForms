using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models;

namespace SproutForms.Core.Registry
{
    public class CodeFormRegistry
    {
        private readonly List<Func<IServiceProvider, ICodeFirstForm>> _factories = [];

        public IReadOnlyList<Func<IServiceProvider, ICodeFirstForm>> Factories => _factories;

        public void Add<T>() where T : class, ICodeFirstForm
            => _factories.Add(sp => ActivatorUtilities.CreateInstance<T>(sp));

        public void Add(ICodeFirstForm instance)
            => _factories.Add(_ => instance);
    }
}
