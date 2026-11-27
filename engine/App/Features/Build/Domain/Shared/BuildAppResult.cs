namespace Build.Domain.Shared;

using Operations.Domain.Shared;

public sealed record BuildAppResult(IReadOnlyList<Finding> Findings, bool Failed = false) : IOperationResult;
