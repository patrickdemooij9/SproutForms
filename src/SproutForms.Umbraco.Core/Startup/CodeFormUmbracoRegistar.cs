using SproutForms.Core.Registry;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace SproutForms.Umbraco.Core.Startup
{
    public class CodeFormUmbracoRegistar : IAsyncComponent
    {
        private readonly IRuntimeState _runtimeState;
        private readonly CodeFormRegistrar _registrar;

        public CodeFormUmbracoRegistar(IRuntimeState runtimeState, CodeFormRegistrar registrar)
        {
            _runtimeState = runtimeState;
            _registrar = registrar;
        }

        public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
        {
            // The tables don't exist until Umbraco is installed and migrated
            if (_runtimeState.Level != RuntimeLevel.Run) return Task.CompletedTask;

            _registrar.RegisterAll();
            return Task.CompletedTask;
        }

        public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
