namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

sealed class Operations(IOperationConverter? converter, IServiceScopeFactory scopes) : IOperations {
    readonly object gate = new();
    readonly CancellationTokenSource stopping = new();
    TaskCompletionSource<bool>? idle;
    int running;
    bool closed;

    public Task<MessageResult> Send(Operation operation, CancellationToken ct = default) {
        if (converter is null) {
            return Task.FromResult(MessageResult.Fail($"No operation converter is registered, so '{operation.Name}' has no message."));
        }
        var converted = converter.Convert(operation);
        return converted.Value is { } message ? SendMessage(message, ct) : Task.FromResult(converted);
    }

    public async Task<MessageResult> SendMessage(object message, CancellationToken ct = default) {
        lock (gate) {
            if (closed) {
                return MessageResult.Fail("The app is stopping.");
            }
            running++;
        }
        try {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, stopping.Token);
            using var scope = scopes.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IHub>().Send(message, linked.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (stopping.IsCancellationRequested) {
            return MessageResult.Fail("The app stopped before the message was done.");
        } finally {
            lock (gate) {
                running--;
                if (running == 0) {
                    idle?.TrySetResult(true);
                }
            }
        }
    }

    internal async Task Drain(TimeSpan grace) {
        Task wait;
        lock (gate) {
            closed = true;
            if (running == 0) {
                return;
            }
            idle = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            wait = idle.Task;
        }
        if (await Task.WhenAny(wait, Task.Delay(grace)).ConfigureAwait(false) != wait) {
            stopping.Cancel();
            await Task.WhenAny(wait, Task.Delay(grace)).ConfigureAwait(false);
        }
    }
}
