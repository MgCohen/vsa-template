namespace Facts.Domain.Shared.Compile;

using Umpire.Config;

public sealed record CompileProject(
    string Name,
    string ConfigPath,
    string ConfigText,
    bool IsFrameworkCode,
    IReadOnlyList<string> SuppressedRules,
    IReadOnlyList<string> References,
    ModelSettings Model) {
    public const string ConfigFile = UmpireConfig.FileName;

    public static CompileProject None { get; } = new("", "", "", false, [], [], new ModelSettings(null, null, null, [], false));
}
