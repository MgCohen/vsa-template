namespace Build.Domain.Shared;

using Steps.Domain.Shared;

public sealed record BuildAppResult(IReadOnlyList<Finding> Findings) : IStepResult;
