using VSA.Contracts.Commands;

namespace VSA.Handlers.Commands;

// The one handler of a command. The host registers each handler of a Handlers.Commands project.
public abstract class CommandHandler<TCommand, TResult> : ICommandHandler<TResult> where TCommand : ICommand<TResult> {
    public abstract Task<TResult> HandleAsync(TCommand command, CancellationToken ct);

    Task<TResult> ICommandHandler<TResult>.HandleAsync(ICommand<TResult> command, CancellationToken ct) => HandleAsync((TCommand)command, ct);
}

// The sender knows only the result type at compile time. This interface lets it call the handler without reflection.
interface ICommandHandler<TResult> {
    Task<TResult> HandleAsync(ICommand<TResult> command, CancellationToken ct);
}
