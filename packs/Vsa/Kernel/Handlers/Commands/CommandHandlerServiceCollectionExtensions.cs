namespace Umpire.Vsa.Handlers.Commands;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Kernels;
using Umpire.Messages.Handlers;

public static class CommandHandlerServiceCollectionExtensions {
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, AppTypes types) {
        EnsureOneHandlerForEachCommand(types.Implementations(typeof(CommandHandler<,>)));
        services.AddScoped<ICommandSender, InProcessCommandSender>();
        services.AddPipeline(new PipelineBuilder<IMessage>()
            .HandlersOf(typeof(CommandHandler<,>))
            .CallsCommandHandlers()
            .HooksOf(typeof(BeforeCommandHook<>), typeof(AfterCommandHook<,>)));
        return services;
    }

    static void EnsureOneHandlerForEachCommand(IReadOnlyList<ServiceImplementation> handlers) {
        foreach (var command in handlers.GroupBy(h => h.Service).Where(g => g.Count() > 1)) {
            var names = command.Select(h => h.Implementation.FullName).ToArray();
            throw new InvalidOperationException(
                $"Command {command.Key.GetGenericArguments()[0].FullName} has two handlers: "
                + $"{names[0]} and {names[1]}. A command has one handler.");
        }
    }
}
