namespace Spike.Core;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

public sealed class AppInstance : IAsyncDisposable {
    static readonly TimeSpan Grace = TimeSpan.FromSeconds(2);

    readonly TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ServiceProvider provider;
    readonly Operations operations;
    readonly IAppLog log;
    readonly AppStop stop;
    readonly List<AppHost> opened = [];
    readonly List<Task> work = [];
    int stopping;

    internal AppInstance(ServiceProvider provider, Operations operations, IAppLog log) {
        this.provider = provider;
        this.operations = operations;
        this.log = log;
        stop = provider.GetRequiredService<AppStop>();
    }

    public Task<int> Completion => completion.Task;

    public void Stop(int exitCode = 0) => stop.Request(exitCode);

    public async ValueTask DisposeAsync() {
        Stop();
        await Completion.ConfigureAwait(false);
    }

    public TaskAwaiter<int> GetAwaiter() => Completion.GetAwaiter();

    internal void Open(IReadOnlyList<AppHost> hosts) {
        try {
            foreach (var host in hosts) {
                opened.Add(host);
                work.Add(Watch(host, host.Open(operations, stop)));
            }
        } catch {
            CloseNow();
            throw;
        }
        if (stop.Listen(code => _ = StopAsync(code)) is { } early) {
            _ = StopAsync(early);
        }
    }

    async Task Watch(AppHost host, Task hostWork) {
        try {
            await hostWork.ConfigureAwait(false);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            log.Error($"{host.GetType().Name} failed: {e.Message}");
            stop.Request(1);
        }
    }

    async Task StopAsync(int exitCode) {
        if (Interlocked.Exchange(ref stopping, 1) == 1) {
            return;
        }
        try {
            await operations.Drain(Grace).ConfigureAwait(false);
            await CloseHosts().ConfigureAwait(false);
            var all = Task.WhenAll(work);
            if (await Task.WhenAny(all, Task.Delay(Grace)).ConfigureAwait(false) != all) {
                log.Warning("A host did not end its work in time.");
            }
            await provider.DisposeAsync().ConfigureAwait(false);
            operations.Dispose();
        } catch (Exception e) {
            log.Error($"The stop failed: {e.Message}");
        } finally {
            completion.TrySetResult(exitCode);
        }
    }

    async Task CloseHosts() {
        for (var i = opened.Count - 1; i >= 0; i--) {
            try {
                await opened[i].Close().ConfigureAwait(false);
            } catch (Exception e) {
                log.Error($"{opened[i].GetType().Name} did not close: {e.Message}");
            }
        }
    }

    void CloseNow() {
        try {
            CloseHosts().GetAwaiter().GetResult();
        } finally {
            provider.Dispose();
            operations.Dispose();
        }
    }
}
