using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VSA.Module;

public sealed class ModuleBuilder(string name, IServiceCollection services, IConfiguration configuration) {
    public string Name => name;
    public IServiceCollection Services => services;
    public IConfiguration Configuration => configuration;

    public ModuleBuilder Replace<TService, TImplementation>() where TImplementation : class, TService {
        var current = services.LastOrDefault(d => d.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException($"{typeof(TService).Name} has no default registration to replace.");
        services.RemoveAll<TService>();
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), current.Lifetime));
        return this;
    }

    internal TSettings Settings<TSettings>(Func<TSettings, IEnumerable<string>> validate)
        where TSettings : class, new() {
        var section = configuration.GetSection($"Modules:{name}");
        var settings = section.Get<TSettings>() ?? new TSettings();
        var errors = SettingsValidator.Errors(settings, validate);
        if (errors.Count > 0) {
            throw new InvalidOperationException($"{section.Path} is not valid: {string.Join(" ", errors)}");
        }
        services.AddSingleton(settings);
        return settings;
    }
}
