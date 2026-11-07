using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Commands;
using VSA.Module;

namespace VSA.Handlers.Commands;

public static class CommandHandlerInstaller {
    public static void Install(VsaContext context) {
        context.Services.AddScoped<ICommandSender, InProcessCommandSender>();
        var handlerOfCommand = new Dictionary<Type, Type>();
        foreach (var (service, handler) in context.Implementations(typeof(CommandHandler<,>))) {
            if (handlerOfCommand.TryGetValue(service, out var other)) {
                throw new InvalidOperationException(
                    $"Command {service.GetGenericArguments()[0].FullName} has two handlers: "
                    + $"{other.FullName} and {handler.FullName}. A command has one handler.");
            }
            handlerOfCommand[service] = handler;
            context.Services.AddScoped(service, handler);
        }
    }
}
