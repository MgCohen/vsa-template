using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace VSA.Module;

public sealed class ModuleBuilder(string name, IServiceCollection services, IConfiguration configuration) {
    public string Name => name;
    public IServiceCollection Services => services;
    public IConfiguration Configuration => configuration;

    // Binds Modules:{Name} to TSettings and checks it. If the settings are not valid, start-up stops.
    // Registers TSettings and IOptions<TSettings>. A reload of IOptions<TSettings> runs the same checks.
    internal TSettings Settings<TSettings>(Func<TSettings, IEnumerable<string>> validate) where TSettings : class, new() {
        var section = configuration.GetSection($"Modules:{name}");
        var settings = section.Get<TSettings>() ?? new TSettings();
        var errors = SettingsValidator<TSettings>.Errors(settings, validate);
        if (errors.Count > 0) {
            throw new InvalidOperationException($"{section.Path} is not valid: {string.Join(" ", errors)}");
        }
        services.AddSingleton(settings);
        services.AddOptions<TSettings>().Bind(section);
        services.AddSingleton<IValidateOptions<TSettings>>(new SettingsValidator<TSettings>(validate));
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
