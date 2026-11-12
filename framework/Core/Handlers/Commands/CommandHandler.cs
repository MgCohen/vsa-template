using VSA.Analyzers.Markers;
using VSA.Contracts;
using VSA.Contracts.Commands;

namespace VSA.Handlers.Commands;

[AutoRegister]
[RequireDerived]
public abstract class CommandHandler<TCommand, TResult> : IHandler<TCommand>
    where TCommand : class, ICommand<TResult> {
    public abstract Task<Result<TResult>> HandleAsync(Envelope<TCommand> envelope);

    async Task<IMessageResult> IHandler.HandleAsync(Envelope envelope) =>
        await HandleAsync((Envelope<TCommand>)envelope);
}
