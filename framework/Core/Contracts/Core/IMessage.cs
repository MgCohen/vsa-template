namespace VSA.Contracts;

// The base of each message that has one handler: a request (IRequest<T>) and a command (ICommand<T>).
// An event is not a message: it has zero or more handlers, and it stays IntegrationEvent.
public interface IMessage;
