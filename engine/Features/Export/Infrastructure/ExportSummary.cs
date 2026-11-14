namespace Export.Infrastructure;

public sealed record ExportSummary(string ArchitectureFile, int TopParts, int Rules, string SnapshotFile, int Types);
