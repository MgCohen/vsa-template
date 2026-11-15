namespace VSA.Kernel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

static class ModuleSettings {
    internal static TSettings Bind<TSettings>(
        IServiceCollection services, string path, Func<TSettings, IEnumerable<string>> validate)
        where TSettings : class, new() {
        var section = ComposedConfiguration.Of(services).GetSection(path);
        var settings = section.Get<TSettings>() ?? new TSettings();
        var errors = SettingsValidator.Errors(settings, validate);
        if (errors.Count > 0) {
            throw new InvalidOperationException($"{section.Path} is not valid: {string.Join(" ", errors)}");
        }
        services.AddSingleton(settings);
        return settings;
    }
}
