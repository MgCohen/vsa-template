using Steps.Domain.Shared;

namespace Rules.Slices.CheckRules;

public sealed record CheckRulesResult(IReadOnlyList<Finding> Findings, int Types) : IStepResult;
