namespace VSA.Architecture.Data;

public sealed record TermNode(string Concept, Depth? Depth, Access? Access, Boundary? Boundary, List<FilterNode> Filters);
