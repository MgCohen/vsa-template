namespace Facts.Domain.Shared.Compile;

public sealed record CodeFile(
    string Path,
    IReadOnlyList<int> Lines,
    IReadOnlyList<CodeComment> Comments,
    IReadOnlyList<CodeName> TopLevelTypes,
    IReadOnlyList<CodeTypeBody> Types,
    IReadOnlyList<CodeMethod> Methods,
    IReadOnlyList<CodeBaseCall> BaseCalls);
