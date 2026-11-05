using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Commands;

namespace VSA.Handlers.Commands;

public static class CommandServiceExtensions {
    // Registers ICommandSender as InProcessCommandSender.
    public static IServiceCollection AddVsaCommands(this IServiceCollection services) {
        services.AddScoped<ICommandSender, InProcessCommandSender>();
        return services;
    }
}
