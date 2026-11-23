namespace Steps.Domain.Shared;

public interface IStepResult {
    IReadOnlyList<Finding> Findings { get; }
}
