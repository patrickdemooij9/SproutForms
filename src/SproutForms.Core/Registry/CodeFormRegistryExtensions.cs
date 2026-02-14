using Microsoft.Extensions.DependencyInjection;

namespace SproutForms.Core.Registry
{
    public static class CodeFormRegistryExtensions
    {
        public static IServiceCollection AddCodeFirstForms(
        this IServiceCollection services,
        Action<CodeFormRegistry> configure)
        {
            var existingDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(CodeFormRegistry));
            
            CodeFormRegistry registry;
            
            if (existingDescriptor != null)
            {
                // Registry already exists, reuse it and add to the configuration
                registry = (CodeFormRegistry)existingDescriptor.ImplementationInstance!;
            }
            else
            {
                // Create new registry and register it as a singleton
                registry = new CodeFormRegistry();
                services.AddSingleton(registry);
            }
            
            configure(registry);
            //services.AddHostedService<CodeFormRegistrar>();

            return services;
        }
    }
}
