namespace ArchitectureViewer.Domain.Shared;

public sealed record LinkChange(ChangeKind Kind, string FromNode, string? FromRow, string ToNode, string? ToRow);
