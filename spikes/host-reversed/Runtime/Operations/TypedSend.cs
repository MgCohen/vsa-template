namespace Spike.Kernels;

public static class TypedSend {
    public static async Task<TResponse> Send<TResponse>(
        this IOperations operations, IRequest<TResponse> request, CancellationToken ct = default) {
        var result = await operations.Invoke(request, ct).ConfigureAwait(false);
        return result.Error is { } error ? throw new InvalidOperationException(error) : (TResponse)result.Value!;
    }
}
