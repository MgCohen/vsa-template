namespace Spike.Core;

public interface IHandlerCall {
    Task<object?> Call(object handler, object message, CancellationToken ct);
}
