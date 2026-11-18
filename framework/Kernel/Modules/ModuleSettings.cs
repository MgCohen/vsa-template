namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;

public sealed class ModuleSettings(IReadOnlyList<ISettingsProvider> providers) {
    public TSettings Get<TSettings>(Module module) where TSettings : class, new() =>
        providers.Select(provider => provider.Get<TSettings>(module)).FirstOrDefault(found => found is not null)
        ?? new TSettings();

    internal TSettings Use<TSettings>(
        IServiceCollection services, Module module, Func<TSettings, IEnumerable<string>> validate)
        where TSettings : class, new() {
        var settings = Get<TSettings>(module);
        var errors = validate(settings).ToList();
        if (errors.Count > 0) {
            throw new InvalidOperationException(
                $"The settings of the module {module.Name} are not valid: {string.Join(" ", errors)}");
        }
        services.AddSingleton(settings);
        return settings;
    }
}
