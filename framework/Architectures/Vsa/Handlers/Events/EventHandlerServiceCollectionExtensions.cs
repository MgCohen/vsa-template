namespace VSA.Handlers.Events;

using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;

public static class EventHandlerServiceCollectionExtensions {
    public static IServiceCollection AddEventHandlers(this IServiceCollection services) {
        services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        services.AddDefaultPipeline(
            PipelineKind.Events,
            new PipelineBuilder<IntegrationEvent>().HandlersOf(typeof(IntegrationEventHandler<>)));
        return services;
    }
}
