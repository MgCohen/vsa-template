namespace Umpire.Messages.Contracts;

public interface IMessageResult {
    bool IsSuccess { get; }
    Error? Error { get; }
    object? Value { get; }
}
