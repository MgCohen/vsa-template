namespace Spike.Pack;

using Spike.Core;

sealed class OperationCall : IHandlerCall {
    public async Task<object?> Call(object handler, object message, CancellationToken ct) =>
        await ((OperationHandler)handler).Handle((Operation)message, ct).ConfigureAwait(false);
}
