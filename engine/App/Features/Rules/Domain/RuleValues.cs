namespace Rules.Domain;

using Operations.Domain.Shared;

public sealed class RuleValues(string code, IReadOnlyDictionary<string, string> values) {
    public string? Value(string key) =>
        values.TryGetValue($"umpire.{code}.{key}", out var rule) ? rule
        : values.TryGetValue($"umpire.{key}", out var global) ? global
        : null;

    public FindingSeverity? Severity(FindingSeverity defaultValue) =>
        values.TryGetValue($"umpire.{code}.severity", out var text) ? text.Trim().ToLowerInvariant() switch {
            "none" or "silent" => null,
            "error" => FindingSeverity.Error,
            "warning" => FindingSeverity.Warning,
            "suggestion" or "info" => FindingSeverity.Info,
            _ => defaultValue,
        } : defaultValue;
}
