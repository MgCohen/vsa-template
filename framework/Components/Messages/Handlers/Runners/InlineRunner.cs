namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public sealed class InlineRunner : IRunner {
    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) => run(envelope);

    public Task StartAsync() => Task.CompletedTask;
}
