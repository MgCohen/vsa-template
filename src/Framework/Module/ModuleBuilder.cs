using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VSA.Module;

public sealed class ModuleBuilder(string name, IServiceCollection services, IConfiguration configuration) {
    public IServiceCollection Services => services;
    public IConfiguration Configuration => configuration;

    // Binds the Modules:{Name} section to TSettings, registers it, and returns it.
    public TSettings Settings<TSettings>() where TSettings : class, new() {
        var settings = new TSettings();
        configuration.GetSection($"Modules:{name}").Bind(settings);
        services.AddSingleton(settings);
        return settings;
    }

    // Removes the default registration of TService and adds TImplementation with the same lifetime.
    public ModuleBuilder Replace<TService, TImplementation>() where TImplementation : class, TService {
        var current = services.LastOrDefault(d => d.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException($"{typeof(TService).Name} has no default registration to replace.");
        for (var i = services.Count - 1; i >= 0; i--) {
            if (services[i].ServiceType == typeof(TService)) {
                services.RemoveAt(i);
            }
        }
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), current.Lifetime));
        return this;
    }
}
