using VSA.Contracts.Commands;
using VSA.Module;

namespace VSA.Handlers.Commands;

public sealed class CommandHandlerInstaller() : LayerInstaller("Handlers.Commands") {
    readonly Dictionary<Type, Type> handlerOfCommand = [];

    public override void Install(VsaContext context) {
        handlerOfCommand.Clear();
        context.Services.AddScoped<ICommandSender, InProcessCommandSender>();
        base.Install(context);
    }

    protected override void Install(VsaContext context, Type type) {
        if (ClosedBase(type, typeof(CommandHandler<,>)) is not { } service) {
            return;
        }
        if (handlerOfCommand.TryGetValue(service, out var other)) {
            throw new InvalidOperationException(
                $"Command {service.GetGenericArguments()[0].FullName} has two handlers: "
                + $"{other.FullName} and {type.FullName}. A command has one handler.");
        }
        handlerOfCommand[service] = type;
        context.Services.Add(service, type, VsaLifetime.Scoped);
    }
}
