namespace Umpire.Vsa.Handlers.Commands;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;

[AutoRegister]
public abstract class CommandHandler<TCommand, TResult> : IRunnableHandler<TCommand>
    where TCommand : class, ICommand<TResult> {
    public abstract Task<Result<TResult>> HandleAsync(IEnvelope<TCommand> envelope);

    async Task<IMessageResult> IRunnableHandler<TCommand>.RunAsync(IEnvelope<TCommand> envelope) =>
        await HandleAsync(envelope);
}
