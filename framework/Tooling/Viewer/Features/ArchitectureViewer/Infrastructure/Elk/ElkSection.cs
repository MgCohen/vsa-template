namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkSection(ElkPoint StartPoint, ElkPoint EndPoint, IReadOnlyList<ElkPoint>? BendPoints);
