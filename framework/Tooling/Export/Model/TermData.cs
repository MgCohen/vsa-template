namespace Viewer.Export.Model;

public sealed record TermData(string Layer, string? Reach, string? Access, List<FilterData> Filters);
