namespace Facts.Domain.Shared.Dll;

public sealed record DllTypeParameter(string Name, IReadOnlyList<string> Limits, IReadOnlyList<DllShape> Types);
