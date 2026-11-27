namespace Spike.Core;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

public sealed class AppInstance : IAsyncDisposable {
    readonly TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ServiceProvider provider;
    readonly Operations operations;
    readonly IAppLog log;
    readonly List<AppHost> opened = [];
    int stopping;

    internal AppInstance(ServiceProvider provider, Operations operations, IAppLog log) {
        this.provider = provider;
        this.operations = operations;
        this.log = log;
    }

    public Task<int> Completion => completion.Task;

    public void Stop(int exitCode = 0) => _ = StopAsync(exitCode);

    public async ValueTask DisposeAsync() {
        await StopAsync(0).ConfigureAwait(false);
        await Completion.ConfigureAwait(false);
    }

    public TaskAwaiter<int> GetAwaiter() => Completion.GetAwaiter();

    internal void Open(IReadOnlyList<AppHost> hosts) {
        var stop = provider.GetRequiredService<AppStop>();
        foreach (var host in hosts) {
            Watch(host, host.Open(operations, stop));
            opened.Add(host);
        }
        if (stop.Listen(Stop) is { } early) {
            Stop(early);
        }
    }

    async void Watch(AppHost host, Task work) {
        try {
            await work.ConfigureAwait(false);
        } catch (Exception e) when (e is not OperationCanceledException) {
            log.Error($"{host.GetType().Name} failed: {e.Message}");
            Stop(1);
        } catch (OperationCanceledException) {
        }
    }

    async Task StopAsync(int exitCode) {
        if (Interlocked.Exchange(ref stopping, 1) == 1) {
            return;
        }
        try {
            await operations.Drain(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            for (var i = opened.Count - 1; i >= 0; i--) {
                await opened[i].Close().ConfigureAwait(false);
            }
            await provider.DisposeAsync().ConfigureAwait(false);
        } catch (Exception e) {
            log.Error($"The stop failed: {e.Message}");
        } finally {
            completion.TrySetResult(exitCode);
        }
    }
}
