namespace Config.Domain.Shared;

using Steps.Domain.Shared;

public sealed record WriteConfigResult(IReadOnlyList<Finding> Findings, string? Copy) : IStepResult;
