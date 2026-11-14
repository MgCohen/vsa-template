using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VSA.Kernel;

public sealed class AnchorBuilder(string name, IServiceCollection services, IConfiguration configuration) {
    public string Name => name;
    public IServiceCollection Services => services;
    public IConfiguration Configuration => configuration;

    public AnchorBuilder Replace<TService, TImplementation>() where TImplementation : class, TService {
        var current = services.LastOrDefault(d => d.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException($"{typeof(TService).Name} has no default registration to replace.");
        services.RemoveAll<TService>();
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), current.Lifetime));
        return this;
    }

    internal TSettings Settings<TSettings>(string section, Func<TSettings, IEnumerable<string>> validate)
        where TSettings : class, new() {
        var settingsSection = configuration.GetSection($"{section}:{name}");
        var settings = settingsSection.Get<TSettings>() ?? new TSettings();
        var errors = SettingsValidator.Errors(settings, validate);
        if (errors.Count > 0) {
            throw new InvalidOperationException($"{settingsSection.Path} is not valid: {string.Join(" ", errors)}");
        }
        services.AddSingleton(settings);
        return settings;
    }
}
