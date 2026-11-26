namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public sealed class LocalHost : AppHost {
    IServiceProvider? services;
    IReadOnlyList<Operation> operations = [];

    public override void Start(IServiceProvider services, AppStop stop) {
        this.services = services;
        operations = [.. services.GetServices<Operation>()];
    }

    public OperationResult Invoke(object request, CancellationToken ct = default) {
        var running = services
            ?? throw new InvalidOperationException("Start the app with this host before you call Invoke.");
        var operation = operations.SingleOrDefault(o => o.Input == request.GetType())
            ?? throw new InvalidOperationException($"The app has no handler for {request.GetType().Name}.");
        using var scope = running.CreateScope();
        return operation.Invoke(scope.ServiceProvider, request, ct).GetAwaiter().GetResult();
    }

    public override void Dispose() {
        services = null;
        operations = [];
        base.Dispose();
    }
}
