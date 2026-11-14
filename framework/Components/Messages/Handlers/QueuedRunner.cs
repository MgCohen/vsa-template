using VSA.Contracts;

namespace VSA.Handlers;

public sealed class QueuedRunner : IRunner {
    readonly RunQueue queue = new();

    public int Count => queue.Count;

    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) => queue.Add(envelope, run, first: false);

    public Task StartAsync() => queue.DrainAsync();
}
