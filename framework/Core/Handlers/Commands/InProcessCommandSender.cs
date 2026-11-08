using VSA.Contracts.Commands;
using VSA.Module;

namespace VSA.Handlers.Commands;

sealed class InProcessCommandSender(IVsaResolver resolver) : ICommandSender {
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct) {
        if (command is null) {
            throw new ArgumentNullException(nameof(command));
        }
        var handlerType = typeof(CommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = resolver.Resolve(handlerType) as ICommandHandler<TResult>
            ?? throw new InvalidOperationException(
                $"Command {command.GetType().FullName} has no handler. Add a CommandHandler<{command.GetType().Name}, "
                + $"{typeof(TResult).Name}> to the Handlers.Commands project of its module.");
        return handler.HandleAsync(command, ct);
    }
}
