using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Kernel;

namespace VSA.Handlers.Commands;

sealed class CommandHandlerInstaller : IInstaller {
    public void Install(IServiceCollection services, AnchorTypes types) {
        EnsureOneHandlerForEachCommand(types.Implementations(typeof(CommandHandler<,>)));
        services.AddScoped<ICommandSender, InProcessCommandSender>();
        services.AddDefaultPipeline(
            PipelineKind.Commands, new PipelineBuilder<IMessage>().HandlersOf(typeof(CommandHandler<,>)));
    }

    static void EnsureOneHandlerForEachCommand(IReadOnlyList<(Type Service, Type Implementation)> handlers) {
        foreach (var command in handlers.GroupBy(h => h.Service).Where(g => g.Count() > 1)) {
            var names = command.Select(h => h.Implementation.FullName).ToArray();
            throw new InvalidOperationException(
                $"Command {command.Key.GetGenericArguments()[0].FullName} has two handlers: "
                + $"{names[0]} and {names[1]}. A command has one handler.");
        }
    }
}
