namespace Operations.Infrastructure;

using Operations.Domain.Shared;
using Umpire.Config;

public static class CheckMode {
    public const string Key = "umpire.check.mode";

    public static ReportMode Of(ReportMode? given, string folder) =>
        given ?? (Enum.TryParse<ReportMode>(Configured(folder), ignoreCase: true, out var mode) && Enum.IsDefined(typeof(ReportMode), mode)
            ? mode
            : ReportMode.Warn);

    static string? Configured(string folder) {
        try {
            return UmpireConfig.Find(folder).Value(Key);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return null;
        }
    }
}
