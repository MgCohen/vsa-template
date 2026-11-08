using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Commands;
using VSA.Module;

namespace VSA.Handlers.Commands;

public static class CommandHandlerInstaller {
    public static void Install(VsaContext context) {
        var handlers = context.Implementations(typeof(CommandHandler<,>));
        EnsureOneHandlerForEachCommand(handlers);
        context.Services.AddScoped<ICommandSender, InProcessCommandSender>();
        foreach (var (service, handler) in handlers) {
            context.Services.AddScoped(service, handler);
        }
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
