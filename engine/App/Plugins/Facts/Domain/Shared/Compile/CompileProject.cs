namespace Facts.Domain.Shared.Compile;

public sealed record CompileProject(
    string Name,
    string ConfigPath,
    string ConfigText,
    bool IsFrameworkCode,
    IReadOnlyList<string> SuppressedRules,
    IReadOnlyList<string> References,
    ModelSettings Model);
