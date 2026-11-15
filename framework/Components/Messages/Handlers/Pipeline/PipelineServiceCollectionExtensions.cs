using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VSA.Kernel;

namespace VSA.Handlers;

public static class PipelineServiceCollectionExtensions {
    public static IServiceCollection AddPipelines(this IServiceCollection services, AppTypes types) {
        services.AddSingleton(new PipelineCatalog(types));
        services.TryAddSingleton(provider => new Pipelines(provider, provider.GetServices<PipelineRegistration>()));
        return services;
    }

    public static IServiceCollection AddPipeline<TMessage>(
        this IServiceCollection services, PipelineBuilder<TMessage> builder) where TMessage : class {
        if (builder is null) {
            throw new ArgumentNullException(nameof(builder));
        }
        if (services.Any(d => d.ImplementationInstance is PipelineRegistration { HandlerBase: null } r
                && r.Message == typeof(TMessage))) {
            throw new InvalidOperationException(
                $"Two pipelines are registered for {typeof(TMessage).FullName}. A message base has one pipeline.");
        }
        Register(services, new PipelineRegistration(typeof(TMessage), null, builder.Build));
        services.AddSingleton(provider => provider.GetRequiredService<Pipelines>().Of<TMessage>());
        return services;
    }

    internal static void AddDefaultPipeline<TMessage>(
        this IServiceCollection services, Type handlerBase, PipelineBuilder<TMessage> builder)
        where TMessage : class {
        if (builder is null) {
            throw new ArgumentNullException(nameof(builder));
        }
        Register(services, new PipelineRegistration(typeof(TMessage), handlerBase, builder.Build));
    }

    static void Register(IServiceCollection services, PipelineRegistration registration) {
        services.AddSingleton(registration);
        services.TryAddSingleton(provider => new Pipelines(provider, provider.GetServices<PipelineRegistration>()));
    }
}
