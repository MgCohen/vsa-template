namespace Umpire.Vsa.Handlers.Commands;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;

public sealed class CommandHandlerCall<TCommand, TResult> : HandlerCall<CommandHandler<TCommand, TResult>, TCommand>
    where TCommand : class, ICommand<TResult> {
    public override async Task<IMessageResult> CallAsync(
        CommandHandler<TCommand, TResult> handler, IEnvelope<TCommand> envelope) =>
        await handler.HandleAsync(envelope);
}
