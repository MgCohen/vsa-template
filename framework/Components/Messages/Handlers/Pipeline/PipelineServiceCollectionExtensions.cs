namespace Umpire.Messages.Handlers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class PipelineServiceCollectionExtensions {
    public static IServiceCollection AddPipeline<TMessage>(
        this IServiceCollection services, PipelineBuilder<TMessage> builder) where TMessage : class {
        if (builder is null) {
            throw new ArgumentNullException(nameof(builder));
        }
        var definition = builder.Definition();
        if (services.Any(d => d.ImplementationInstance is PipelineDefinition other && other.Overlaps(definition))) {
            throw new InvalidOperationException(
                $"Two pipelines are registered for {typeof(TMessage).FullName} with the same handler base. "
                + "A message base has one pipeline for each handler base. "
                + "The default pipeline of each kind already has its message base (IMessage or IntegrationEvent) "
                + "with its handler base, so give a module pipeline a narrower message base.");
        }
        services.AddSingleton(definition);
        AddHub(services);
        return services;
    }

    public static IServiceCollection AddHubStep<TStep>(this IServiceCollection services, int priority = 0) where TStep : HubStep {
        if (services.Any(d => d.ImplementationInstance is HubStepEntry entry && entry.Step == typeof(TStep))) {
            throw new InvalidOperationException($"{typeof(TStep).FullName} is already a hub step. Add each hub step one time.");
        }
        services.AddSingleton(new HubStepEntry(typeof(TStep), priority));
        AddHub(services);
        return services;
    }

    static void AddHub(IServiceCollection services) {
        services.TryAddSingleton(provider => new Pipelines(
            provider, provider.GetServices<PipelineDefinition>(), provider.GetServices<HubStepEntry>()));
        services.TryAddScoped<IMessageHub, MessageHub>();
    }
}
