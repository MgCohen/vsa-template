namespace Umpire.CheckModel;

public sealed record AttributeFact(TypeName Type, IReadOnlyList<TypeName> Bases, IReadOnlyList<string> Arguments, string? Member = null);
