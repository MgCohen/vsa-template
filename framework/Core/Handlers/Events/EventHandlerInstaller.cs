using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Handlers.Events;

static class EventHandlerInstaller {
    internal static void Install(IServiceCollection services, ModuleTypes types) {
        services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        services.AddDefaultPipeline(
            PipelineKind.Events,
            new PipelineBuilder<IntegrationEvent>().HandlersOf(typeof(IntegrationEventHandler<>)));
    }
}
