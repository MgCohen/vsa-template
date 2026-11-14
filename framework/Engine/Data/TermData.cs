namespace VSA.Architecture.Data;

public sealed record TermData(string Layer, string? Depth, string? Access, List<FilterData> Filters);
