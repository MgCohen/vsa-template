using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

public sealed class QueuedRunner : IRunner {
    readonly RunQueue queue = new();

    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) => queue.Add(envelope, run, first: false);

    public Task StartAsync() => queue.DrainAsync();
}
