namespace Spike.Core;

public interface IOperations {
    Task<MessageResult> Send(Operation operation, CancellationToken ct = default);
}
