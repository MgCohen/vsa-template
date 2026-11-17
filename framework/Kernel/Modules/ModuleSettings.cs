namespace Umpire.Kernel;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

static class ModuleSettings {
    internal static TSettings Bind<TSettings>(
        IServiceCollection services, IConfigurationSection section, Func<TSettings, IEnumerable<string>> validate)
        where TSettings : class, new() {
        var settings = section.Get<TSettings>() ?? new TSettings();
        var errors = Errors(settings, validate);
        if (errors.Count > 0) {
            throw new InvalidOperationException($"{section.Path} is not valid: {string.Join(" ", errors)}");
        }
        services.AddSingleton(settings);
        return settings;
    }

    static List<string> Errors<TSettings>(TSettings settings, Func<TSettings, IEnumerable<string>> validate)
        where TSettings : class {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
        return [.. results.Select(r => r.ErrorMessage ?? "A value is not valid."), .. validate(settings)];
    }
}
