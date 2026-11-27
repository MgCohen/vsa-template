namespace Changes.Domain.Shared;

using Operations.Domain.Shared;

public sealed record ChangesResult(IReadOnlyList<Finding> Findings, bool Same, string Hash) : IOperationResult;
