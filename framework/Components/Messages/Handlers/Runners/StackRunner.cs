namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public sealed class StackRunner : IRunner {
    readonly RunQueue queue = new();

    public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) {
        if (queue.Draining && envelope.Parent is { } parent) {
            return parent.Stage is EnvelopeStage.AfterHooks or EnvelopeStage.AfterSteps
                ? queue.Add(envelope, run, first: true)
                : run(envelope);
        }
        var done = queue.Add(envelope, run, first: false);
        return queue.Draining ? done : DrainThenAsync(done);
    }

    public Task StartAsync() => queue.DrainAsync();

    async Task DrainThenAsync(Task done) {
        await queue.DrainAsync();
        await done;
    }
}
