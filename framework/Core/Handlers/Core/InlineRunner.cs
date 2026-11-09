using VSA.Contracts;

namespace VSA.Handlers;

public sealed class InlineRunner : IRunner {
    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) => run(envelope);

    public Task StartAsync() => Task.CompletedTask;
}
