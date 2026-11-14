namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkEdge(string Id, IReadOnlyList<string> Sources, IReadOnlyList<string> Targets);
