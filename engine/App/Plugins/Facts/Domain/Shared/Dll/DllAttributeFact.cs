namespace Facts.Domain.Shared.Dll;

public sealed record DllAttributeFact(TypeName Type, IReadOnlyList<TypeName> Bases, IReadOnlyList<string> Arguments, string? Member);
