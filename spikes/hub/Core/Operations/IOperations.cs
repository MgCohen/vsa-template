namespace Spike.Core;

public interface IOperations {
    Task<MessageResult> Send(Operation operation, CancellationToken ct = default);

    Task<MessageResult> SendMessage(object message, CancellationToken ct = default);
}
