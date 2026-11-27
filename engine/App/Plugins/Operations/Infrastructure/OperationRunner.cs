namespace Operations.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class OperationRunner : IRunner {
    public async Task RunAsync(Envelope envelope, Func<Envelope, Task> run) {
        try {
            await run(envelope);
        } catch (Exception e) when (e is not OperationCanceledException && !FailFast(envelope)) {
            return;
        }
    }

    static bool FailFast(Envelope envelope) => envelope.Services.GetRequiredService<RunOptions>().FailFast;
}
