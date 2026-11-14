using Definition.Domain.Shared;
using Steps.Contracts.Commands;

namespace Export.Slices.ExportDefinition;

public sealed record ExportDefinitionCommand(LoadedApp App, string File) : EngineStep<ExportDefinitionResult>;
