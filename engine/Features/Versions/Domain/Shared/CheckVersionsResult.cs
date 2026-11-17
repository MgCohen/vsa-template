namespace Versions.Domain.Shared;

using Steps.Domain.Shared;

public sealed record CheckVersionsResult(IReadOnlyList<Finding> Findings) : IStepResult;
