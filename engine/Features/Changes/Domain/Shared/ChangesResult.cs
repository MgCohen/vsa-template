namespace Changes.Domain.Shared;

using Steps.Domain.Shared;

public sealed record ChangesResult(IReadOnlyList<Finding> Findings, bool Same, string Hash) : IStepResult;
