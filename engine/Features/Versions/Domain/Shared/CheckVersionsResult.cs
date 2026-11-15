using Steps.Domain.Shared;

namespace Versions.Domain.Shared;

public sealed record CheckVersionsResult(string Version, IReadOnlyList<Finding> Findings) : IStepResult;
