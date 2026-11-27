namespace Model.Slices.ExportModel;

using Operations.Domain.Shared;

public sealed record ExportModelResult(IReadOnlyList<Finding> Findings) : IOperationResult;
