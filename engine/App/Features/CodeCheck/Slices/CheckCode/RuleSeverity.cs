namespace CodeCheck.Slices.CheckCode;

using Operations.Domain.Shared;

static class RuleSeverity {
    public const string Key = "severity";

    public static string? Text(CodeConfig config, string ruleId) => config.Value($"umpire.{ruleId}.{Key}");

    public static bool IsOff(string? text) => Normal(text) is "none" or "silent";

    public static FindingSeverity? Of(string? text) => Normal(text) switch {
        "error" => FindingSeverity.Error,
        "warning" => FindingSeverity.Warning,
        "suggestion" or "info" => FindingSeverity.Info,
        _ => null,
    };

    static string? Normal(string? text) => text?.Trim().ToLowerInvariant();
}
