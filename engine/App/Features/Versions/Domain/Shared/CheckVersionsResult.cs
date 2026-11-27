namespace Versions.Domain.Shared;

using Operations.Domain.Shared;

public sealed record CheckVersionsResult(IReadOnlyList<Finding> Findings) : IOperationResult;
