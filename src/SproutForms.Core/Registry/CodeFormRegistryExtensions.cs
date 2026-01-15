using Microsoft.Extensions.DependencyInjection;

namespace SproutForms.Core.Registry
{
    public static class CodeFormRegistryExtensions
    {
        public static IServiceCollection AddCodeFirstForms(
        this IServiceCollection services,
        Action<CodeFormRegistry> configure)
        {
            var registry = new CodeFormRegistry();
            configure(registry);

            services.AddSingleton(registry);
            //services.AddHostedService<CodeFormRegistrar>();

            return services;
        }
    }
}
