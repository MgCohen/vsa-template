namespace Baseline.Slices.ApplyBaseline;

using Steps.Domain.Shared;

public sealed record ApplyBaselineResult(IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Accepted)
    : IStepResult;
