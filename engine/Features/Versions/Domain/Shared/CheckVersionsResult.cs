namespace Versions.Domain.Shared;

using Steps.Domain.Shared;

public sealed record CheckVersionsResult(string Version, IReadOnlyList<Finding> Findings) : IStepResult;
