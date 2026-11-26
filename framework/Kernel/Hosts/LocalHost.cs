namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;

public class LocalHost : AppHost {
    readonly object gate = new();
    readonly ManualResetEventSlim noSends = new(initialState: true);
    IServiceProvider? services;
    IReadOnlyList<Operation> operations = [];
    int sends;

    public override void Start(IServiceProvider services, AppStop stop) {
        lock (gate) {
            operations = [.. services.GetServices<Operation>()];
            this.services = services;
        }
    }

    public override void Dispose() {
        lock (gate) {
            services = null;
            operations = [];
        }
        if (!HostWork.IsCurrent) {
            noSends.Wait();
        }
        base.Dispose();
    }

    internal async Task<OperationResult> SendAsync(object message, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        var operation = Begin(message.GetType(), out var provider);
        try {
            HostWork.MarkCurrentFlow();
            using var scope = provider.CreateScope();
            return await operation.Invoke(scope.ServiceProvider, message, ct).ConfigureAwait(false);
        } finally {
            End();
        }
    }

    Operation Begin(Type input, out IServiceProvider provider) {
        lock (gate) {
            provider = services ?? throw LocalHostApps.NotRunning(GetType().Name);
            var operation = OperationOf(input);
            sends++;
            noSends.Reset();
            return operation;
        }
    }

    void End() {
        lock (gate) {
            sends--;
            if (sends == 0) {
                noSends.Set();
            }
        }
    }

    Operation OperationOf(Type input) {
        for (var type = input; type is not null; type = type.BaseType) {
            var found = operations.Where(operation => operation.Input == type).ToList();
            if (found.Count == 1) {
                return found[0];
            }
            if (found.Count > 1) {
                throw new InvalidOperationException(
                    $"{GetType().Name} has {found.Count} operations for {type.FullName}. Give the request one handler.");
            }
        }
        throw new InvalidOperationException(
            $"{GetType().Name} has no operation for {input.FullName}. A host exposes only requests: "
            + "send a request that has a request handler in a module of the app.");
    }
}
