namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;

public sealed class LocalHost : AppHost {
    IServiceProvider? services;
    IReadOnlyDictionary<Type, Operation>? operations;

    public override void Start(IServiceProvider services, AppStop stop) {
        this.services = services;
        operations = services.GetServices<Operation>().ToDictionary(operation => operation.Input);
    }

    internal async Task<OperationResult> SendAsync(object message, CancellationToken ct) {
        var provider = services ?? throw new InvalidOperationException("The app is not running. Call Start first.");
        if (!operations!.TryGetValue(message.GetType(), out var operation)) {
            throw new InvalidOperationException($"No operation takes {message.GetType().FullName}.");
        }
        using var scope = provider.CreateScope();
        return await operation.Invoke(scope.ServiceProvider, message, ct).ConfigureAwait(false);
    }
}
