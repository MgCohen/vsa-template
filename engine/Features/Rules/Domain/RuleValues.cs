namespace Rules.Domain;

using Steps.Domain.Shared;

public sealed class RuleValues(string code, IReadOnlyDictionary<string, string> values) {
    public string? Value(string key) =>
        values.TryGetValue($"umpire.{code}.{key}", out var rule) ? rule
        : values.TryGetValue($"umpire.{key}", out var global) ? global
        : null;

    public FindingSeverity? Severity(FindingSeverity defaultValue) =>
        values.TryGetValue($"dotnet_diagnostic.{code}.severity", out var text) ? text.Trim().ToLowerInvariant() switch {
            "none" or "silent" => null,
            "error" => FindingSeverity.Error,
            "warning" => FindingSeverity.Warning,
            "suggestion" => FindingSeverity.Info,
            _ => defaultValue,
        } : defaultValue;
}
