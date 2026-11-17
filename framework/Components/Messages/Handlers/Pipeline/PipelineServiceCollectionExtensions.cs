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
        if (services.Any(d => d.ImplementationInstance is PipelineDefinition other
                && other.Message == definition.Message
                && (other.HandlerBases.Intersect(definition.HandlerBases).Any()
                    || (other.HandlerBases.Count == 0 && definition.HandlerBases.Count == 0)))) {
            throw new InvalidOperationException(
                $"Two pipelines are registered for {typeof(TMessage).FullName}. "
                + "A message base has one pipeline for each handler base.");
        }
        services.AddSingleton(definition);
        services.TryAddSingleton(provider => new Pipelines(provider, provider.GetServices<PipelineDefinition>()));
        return services;
    }
}
