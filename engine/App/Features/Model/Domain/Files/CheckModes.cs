namespace Umpire.CheckModel;

public static class CheckModes {
    public const string Key = "umpire.check.mode";
    public const CheckMode Default = CheckMode.Warn;

    public static CheckMode Of(string? text) => Parse(text) ?? Default;

    public static CheckMode? Parse(string? text) =>
        Enum.TryParse<CheckMode>(text, ignoreCase: true, out var mode) && Enum.IsDefined(typeof(CheckMode), mode) ? mode : null;
}
