namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

sealed class Operations(IEnumerable<Operation> operations, IServiceScopeFactory scopes) : IOperations {
    public IReadOnlyList<Operation> All { get; } = [.. operations];

    public async Task<OperationResult> Invoke(object request, CancellationToken ct = default) {
        var operation = All.SingleOrDefault(o => o.Input == request.GetType());
        if (operation is null) {
            return new OperationResult(null, $"The app has no handler for {request.GetType().Name}.", 2);
        }
        using var scope = scopes.CreateScope();
        try {
            return await operation.Invoke(scope.ServiceProvider, request, ct).ConfigureAwait(false);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return new OperationResult(null, $"{operation.Name} failed: {e.Message}", 1);
        }
    }
}
