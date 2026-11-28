namespace Model.Slices.CheckTypes;

using Operations.Domain.Shared;

public sealed record CheckTypesResult(IReadOnlyList<Finding> Findings, bool InModel) : IOperationResult {
    public IReadOnlyList<FindingCode> Codes { get; init; } = [];
}
