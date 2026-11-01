using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace VSA.Module;

// Data annotations first, then the Validate method of the module.
sealed class SettingsValidator<TSettings>(Func<TSettings, IEnumerable<string>> validate) : IValidateOptions<TSettings>
    where TSettings : class {
    public ValidateOptionsResult Validate(string? name, TSettings options) {
        var errors = Errors(options, validate);
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    internal static List<string> Errors(TSettings settings, Func<TSettings, IEnumerable<string>> validate) {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
        return [.. results.Select(r => r.ErrorMessage ?? "A value is not valid."), .. validate(settings)];
    }
}
