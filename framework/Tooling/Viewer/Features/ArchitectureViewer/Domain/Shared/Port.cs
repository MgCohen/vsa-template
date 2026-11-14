namespace ArchitectureViewer.Domain.Shared;

public sealed record Port(string Id, string? RowId, PortSide Side, double Y);
