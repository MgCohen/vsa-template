namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

interface IHandlerCall {
    Type Handler { get; }

    Task<IMessageResult> CallAsync(object handler, Envelope envelope);
}
