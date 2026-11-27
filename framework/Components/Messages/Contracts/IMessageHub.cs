namespace Umpire.Messages.Contracts;

public interface IMessageHub {
    Task<IMessageResult> SendAsync(object message, CancellationToken ct = default);
}
