using System.ComponentModel.DataAnnotations;

namespace VSA.Kernel;

static class SettingsValidator {
    internal static List<string> Errors<TSettings>(TSettings settings, Func<TSettings, IEnumerable<string>> validate)
        where TSettings : class {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
        return [.. results.Select(r => r.ErrorMessage ?? "A value is not valid."), .. validate(settings)];
    }
}
