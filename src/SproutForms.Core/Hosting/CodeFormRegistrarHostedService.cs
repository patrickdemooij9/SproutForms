using Microsoft.Extensions.Hosting;
using SproutForms.Core.Registry;

namespace SproutForms.Core.Hosting
{
    /// <summary>
    /// Registers the code-first forms when a site without Umbraco starts.
    /// </summary>
    public class CodeFormRegistrarHostedService : IHostedService
    {
        private readonly CodeFormRegistrar _registrar;

        public CodeFormRegistrarHostedService(CodeFormRegistrar registrar)
        {
            _registrar = registrar;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _registrar.RegisterAll();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
