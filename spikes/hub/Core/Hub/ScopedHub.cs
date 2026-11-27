namespace Spike.Core;

sealed class ScopedHub(MessageHub hub, IServiceProvider scope) : IHub {
    public Task<MessageResult> Send(object message, CancellationToken ct = default) => hub.Send(message, scope, ct);
}
