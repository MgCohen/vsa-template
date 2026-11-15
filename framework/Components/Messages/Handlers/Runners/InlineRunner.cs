using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

public sealed class InlineRunner : IRunner {
    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) => run(envelope);

    public Task StartAsync() => Task.CompletedTask;
}
