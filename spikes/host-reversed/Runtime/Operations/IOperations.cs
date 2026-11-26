namespace Spike.Kernels;

public interface IOperations {
    IReadOnlyList<Operation> All { get; }

    Task<OperationResult> Invoke(object request, CancellationToken ct = default);
}
