namespace Model.Slices.CheckModel;

using Operations.Domain.Shared;

public sealed record CheckModelResult(IReadOnlyList<Finding> Findings, ReportMode Mode) : IOperationResult;
