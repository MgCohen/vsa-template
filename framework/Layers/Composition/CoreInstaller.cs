using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Composition;

public sealed class CoreInstaller : IVsaInstaller {
    public void Install(VsaContext context) {
        context.Services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        context.Services.AddScoped<ICommandSender, InProcessCommandSender>();
    }
}
