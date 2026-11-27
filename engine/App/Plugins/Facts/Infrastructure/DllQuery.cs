namespace Facts.Infrastructure;

public sealed record DllQuery(IReadOnlyList<string> Dlls, IReadOnlyList<string> Resolve, string? SourceRoot = null);
