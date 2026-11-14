using Steps.Domain.Shared;

namespace Export.Slices.ExportFiles;

public sealed record ExportFilesResult(string Folder, IReadOnlyList<Finding> Findings) : IStepResult;
