using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Commands;

namespace VSA.Handlers.Commands;

sealed class InProcessCommandSender(IServiceProvider provider) : ICommandSender {
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct) {
        if (command is null) {
            throw new ArgumentNullException(nameof(command));
        }
        var handlerType = typeof(CommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = provider.GetService(handlerType) as ICommandHandler<TResult>
            ?? throw new InvalidOperationException(
                $"Command {command.GetType().FullName} has no handler. Add a CommandHandler<{command.GetType().Name}, "
                + $"{typeof(TResult).Name}> to the Handlers.Commands project of its module.");
        return handler.HandleAsync(command, ct);
    }
}
