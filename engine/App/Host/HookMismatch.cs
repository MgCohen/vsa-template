namespace UmpireEngine;

using Operations.Domain.Shared;
using Operations.Infrastructure;
using Umpire.Engine.Versions;
using Umpire.Kernels;

public static class HookMismatch {
    public const string Message =
        "The engine got no job word, for example 'jobs build', so the hook that called it is of another Umpire "
        + "version. Give the packages Umpire and Umpire.Engine the same version, or run 'umpire update'.";

    public static Finding Finding { get; } = new(VersionCheck.Code, FindingSeverity.Error, Message);

    public static bool HasNoJobWord(string[] args) => args.Length == 0 || args[0].StartsWith('-');

    public static ReportMode Mode(HostArguments arguments) {
        var folder = arguments["block"] is { Length: > 0 } block ? block
            : arguments["project"] is { Length: > 0 } project ? project
            : Directory.GetCurrentDirectory();
        ReportMode? given = Enum.TryParse<ReportMode>(arguments["mode"], ignoreCase: true, out var mode)
            ? mode
            : null;
        return CheckMode.Of(given, folder);
    }
}
