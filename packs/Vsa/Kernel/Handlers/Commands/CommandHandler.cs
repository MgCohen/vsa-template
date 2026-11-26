namespace Umpire.Vsa.Handlers.Commands;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;

[AutoRegister]
public abstract class CommandHandler<TCommand, TResult> : IHandler<TCommand>
    where TCommand : class, ICommand<TResult> {
    public abstract Task<Result<TResult>> HandleAsync(IEnvelope<TCommand> envelope);
}
