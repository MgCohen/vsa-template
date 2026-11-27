namespace Spike.Core;

public interface IHub {
    Task<MessageResult> Send(object message, CancellationToken ct = default);
}
