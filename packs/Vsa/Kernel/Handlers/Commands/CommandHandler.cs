namespace Umpire.Vsa.Handlers.Commands;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class CommandHandler<TCommand, TResult> : IHandler<TCommand>
    where TCommand : class, ICommand<TResult> {
    public abstract Task<Result<TResult>> HandleAsync(Envelope<TCommand> envelope);

    async Task<IMessageResult> IHandler.HandleAsync(Envelope envelope) =>
        await HandleAsync((Envelope<TCommand>)envelope);
}
