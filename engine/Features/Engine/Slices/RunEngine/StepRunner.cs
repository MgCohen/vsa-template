using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;
using VSA.Handlers;

namespace Engine.Slices.RunEngine;

public sealed class StepRunner : IRunner {
    public async Task RunAsync(Envelope envelope, Func<Envelope, Task> run) {
        try {
            await run(envelope);
        } catch (Exception e) when (e is not OperationCanceledException && !FailFast(envelope)) {
            return;
        }
    }

    public Task StartAsync() => Task.CompletedTask;

    static bool FailFast(Envelope envelope) => envelope.Services.GetRequiredService<StepLog>().FailFast;
}
