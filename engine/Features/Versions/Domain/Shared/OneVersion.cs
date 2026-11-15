using Steps.Domain.Shared;

namespace Versions.Domain.Shared;

public sealed record OneVersion(string Version, IReadOnlyList<Finding> Findings) : IStepResult;
