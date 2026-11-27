namespace Spike.Hosts.Local;

using Spike.Core;

public sealed class LocalHost : AppHost {
    IOperations? operations;

    public override Task Open(IOperations operations, AppStop stop) {
        this.operations = operations;
        return Task.CompletedTask;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        var opened = operations ?? throw new InvalidOperationException("Run the app with this host before you send.");
        var result = await opened.SendMessage(request, ct).ConfigureAwait(false);
        return result.Error is { } error ? throw new InvalidOperationException(error) : (TResponse)result.Value!;
    }

    public TResponse Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try {
            return SendAsync(request, ct).GetAwaiter().GetResult();
        } finally {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }
}
