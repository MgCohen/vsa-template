namespace VSA.Architecture.Data;

public sealed record NamedTerm(string Concept, IReadOnlyList<NamedFilter> Filters);
