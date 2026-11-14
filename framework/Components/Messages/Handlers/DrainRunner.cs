using VSA.Contracts;

namespace VSA.Handlers;

public sealed class DrainRunner : IRunner {
    readonly RunQueue queue = new();

    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) {
        var done = queue.Add(envelope, run, first: false);
        return queue.Draining ? done : DrainThenAsync(done);
    }

    public Task StartAsync() => queue.DrainAsync();

    async Task DrainThenAsync(Task done) {
        await queue.DrainAsync();
        await done;
    }
}
