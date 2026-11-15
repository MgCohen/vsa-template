using Steps.Domain.Shared;

namespace Baseline.Slices.ApplyBaseline;

public sealed record ApplyBaselineResult(IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Accepted)
    : IStepResult;
