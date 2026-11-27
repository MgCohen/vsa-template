namespace Rules.Slices.CheckRules;

using Operations.Domain.Shared;

public sealed record CheckRulesResult(IReadOnlyList<Finding> Findings) : IOperationResult;
