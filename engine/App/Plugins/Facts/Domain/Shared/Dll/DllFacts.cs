namespace Facts.Domain.Shared.Dll;

public sealed record DllFacts(string Assembly, string Path, IReadOnlyList<DllSourceFile> Files, IReadOnlyList<DllType> Types);
