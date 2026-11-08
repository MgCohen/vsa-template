using Microsoft.Extensions.Configuration;

namespace VSA.Module;

public sealed class ModuleBuilder(string name, IVsaServices services, IConfiguration configuration) {
    public string Name => name;
    public IVsaServices Services => services;
    public IConfiguration Configuration => configuration;

    public ModuleBuilder Replace<TService, TImplementation>() where TImplementation : class, TService {
        var lifetime = services.LifetimeOf(typeof(TService))
            ?? throw new InvalidOperationException($"{typeof(TService).Name} has no default registration to replace.");
        services.RemoveAll(typeof(TService));
        services.Add(typeof(TService), typeof(TImplementation), lifetime);
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
        services.AddInstance(typeof(TSettings), settings);
        return settings;
    }
}
