namespace Spike.Hosts.Local;

using Spike.Kernels;

public sealed class LocalHost : AppHost {
    IOperations? operations;

    public override void Open(IOperations operations, AppStop stop) => this.operations = operations;

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) =>
        Opened.Send(request, ct);

    public TResponse Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) =>
        SendAsync(request, ct).GetAwaiter().GetResult();

    public override void Dispose() {
        operations = null;
        base.Dispose();
    }

    IOperations Opened => operations
        ?? throw new InvalidOperationException("Run the app with this host before you send a request.");
}
