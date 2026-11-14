using Steps.Domain.Shared;

namespace Export.Slices.ExportDefinition;

public sealed record ExportDefinitionResult(string File, IReadOnlyList<Finding> Findings) : IStepResult;
