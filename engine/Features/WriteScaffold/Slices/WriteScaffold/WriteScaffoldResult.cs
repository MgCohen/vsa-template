namespace WriteScaffold.Slices.WriteScaffold;

using Steps.Domain.Shared;

public sealed record WriteScaffoldResult(IReadOnlyList<Finding> Findings) : IStepResult;
