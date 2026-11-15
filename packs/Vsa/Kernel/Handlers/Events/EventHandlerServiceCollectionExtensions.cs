namespace VSA.Handlers.Events;

using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;

public static class EventHandlerServiceCollectionExtensions {
    public static IServiceCollection AddEventHandlers(this IServiceCollection services) {
        services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        services.AddDefaultPipeline(typeof(IntegrationEventHandler<>), new PipelineBuilder<IntegrationEvent>()
            .HandlersOf(typeof(IntegrationEventHandler<>))
            .HooksOf(typeof(BeforeEventHook<>), typeof(AfterEventHook<>)));
        return services;
    }
}
