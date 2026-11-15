namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public sealed class QueuedRunner : IRunner {
    readonly RunQueue queue = new();

    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) => queue.Add(envelope, run, first: false);

    public Task StartAsync() => queue.DrainAsync();
}
