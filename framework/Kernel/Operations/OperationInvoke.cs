namespace Umpire.Kernels;

public delegate Task<OperationResult> OperationInvoke(IServiceProvider services, object input, CancellationToken ct);
