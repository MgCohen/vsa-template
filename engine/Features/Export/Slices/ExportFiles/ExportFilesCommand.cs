using Definition.Domain.Shared;
using Steps.Contracts.Commands;

namespace Export.Slices.ExportFiles;

public sealed record ExportFilesCommand(LoadedApp App, string Folder) : EngineStep<ExportFilesResult>;
