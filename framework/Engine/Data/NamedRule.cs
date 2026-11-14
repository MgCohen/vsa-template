namespace VSA.Architecture.Data;

public sealed record NamedRule(string Text, string Source, Permission Permission, IReadOnlyList<NamedTerm> From, IReadOnlyList<NamedTerm> To);
