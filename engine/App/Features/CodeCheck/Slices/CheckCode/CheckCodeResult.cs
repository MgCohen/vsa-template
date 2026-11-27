namespace CodeCheck.Slices.CheckCode;

using Operations.Domain.Shared;

public sealed record CheckCodeResult(IReadOnlyList<Finding> Findings, bool ChecksTypes) : IOperationResult;
