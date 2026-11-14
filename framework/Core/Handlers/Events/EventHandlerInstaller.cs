using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Kernel;

namespace VSA.Handlers.Events;

sealed class EventHandlerInstaller : IInstaller {
    public void Install(IServiceCollection services, AnchorTypes types) {
        services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        services.AddDefaultPipeline(
            PipelineKind.Events,
            new PipelineBuilder<IntegrationEvent>().HandlersOf(typeof(IntegrationEventHandler<>)));
    }
}
