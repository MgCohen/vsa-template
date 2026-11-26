namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

interface IHandlerCall {
    Task<IMessageResult> CallAsync(object handler, Envelope envelope);
}
