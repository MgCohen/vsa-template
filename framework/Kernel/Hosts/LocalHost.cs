namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

public class LocalHost : AppHost {
    readonly object gate = new();
    readonly ManualResetEventSlim noSends = new(initialState: true);
    IServiceProvider? services;
    IOperationConverter? converter;
    int sends;

    public override void Start(IServiceProvider services, AppStop stop) {
        lock (gate) {
            converter = services.GetService<IOperationConverter>();
            this.services = services;
        }
    }

    public override void Dispose() {
        lock (gate) {
            services = null;
            converter = null;
        }
        if (!HostWork.IsCurrent) {
            noSends.Wait();
        }
        base.Dispose();
    }

    internal async Task<IMessageResult> SendAsync(object message, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        var provider = Begin(message.GetType());
        try {
            HostWork.MarkCurrentFlow();
            using var scope = provider.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IMessageHub>().SendAsync(message, ct).ConfigureAwait(false);
        } finally {
            End();
        }
    }

    internal async Task<IMessageResult> SendAsync(Operation operation, CancellationToken ct) {
        var provider = Begin(null);
        try {
            HostWork.MarkCurrentFlow();
            using var scope = provider.CreateScope();
            return await scope.ServiceProvider.SendAsync(operation, ct).ConfigureAwait(false);
        } finally {
            End();
        }
    }

    IServiceProvider Begin(Type? message) {
        lock (gate) {
            var provider = services ?? throw LocalHostApps.NotRunning(GetType().Name);
            if (message is not null && converter?.Accepts(message) != true) {
                throw new InvalidOperationException(
                    $"{GetType().Name} does not expose {message.FullName}. A host exposes only requests: "
                    + "send a request that has a request handler in a module of the app.");
            }
            sends++;
            noSends.Reset();
            return provider;
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
}
