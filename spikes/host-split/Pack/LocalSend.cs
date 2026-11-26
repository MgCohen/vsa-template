namespace Spike.Pack;

using Spike.Kernels;

public static class LocalSend {
    public static TResponse Send<TResponse>(this LocalHost host, IRequest<TResponse> request, CancellationToken ct = default) {
        var result = host.Invoke(request, ct);
        return result.Error is { } error
            ? throw new InvalidOperationException(error)
            : (TResponse)result.Value!;
    }
}
