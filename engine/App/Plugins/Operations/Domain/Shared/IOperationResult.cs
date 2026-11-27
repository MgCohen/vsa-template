namespace Operations.Domain.Shared;

public interface IOperationResult {
    IReadOnlyList<Finding> Findings { get; }
}
