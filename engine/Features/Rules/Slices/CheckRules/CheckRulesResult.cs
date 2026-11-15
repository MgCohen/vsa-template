namespace Rules.Slices.CheckRules;

using Steps.Domain.Shared;

public sealed record CheckRulesResult(IReadOnlyList<Finding> Findings, int Types) : IStepResult;
