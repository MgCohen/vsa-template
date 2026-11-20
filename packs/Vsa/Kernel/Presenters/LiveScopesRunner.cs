namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.Logging;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class LiveScopesRunner(LiveScopes live, ILogger<LiveScopesRunner> logger) : IRunner {
    public async Task RunAsync(Envelope envelope, Func<Envelope, Task> run) {
        foreach (var scope in live.Now()) {
            await scope.DeliverInTurnAsync(() => RunInAsync(scope, envelope.InScope(scope.Services), run));
        }
    }

    async Task RunInAsync(PresenterScope scope, Envelope envelope, Func<Envelope, Task> run) {
        if (!live.IsLive(scope)) {
            return;
        }
        try {
            await run(envelope);
        } catch (OperationCanceledException) when (envelope.Cancellation.IsCancellationRequested) {
            return;
        } catch (Exception e) when (live.IsLive(scope)) {
            logger.LogError(e, "The presenters failed on {Event}.", envelope.Message.GetType().Name);
            return;
        } catch (Exception) {
            return;
        }
        if (envelope.Error is { } error && live.IsLive(scope)) {
            logger.LogError(
                "The presenters failed on {Event}: {Error}", envelope.Message.GetType().Name, error.Message);
        }
    }
}
