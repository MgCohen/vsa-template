using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VSA.Handlers;

public static class PipelineServiceCollectionExtensions {
    public static IServiceCollection AddPipeline<TMessage>(
        this IServiceCollection services, IPipelineBuilder<TMessage> builder) where TMessage : class {
        if (services.Any(d => d.ImplementationInstance is PipelineRegistration { Kind: null } r
                && r.Message == typeof(TMessage))) {
            throw new InvalidOperationException(
                $"Two pipelines are registered for {typeof(TMessage).FullName}. A message base has one pipeline.");
        }
        Register(services, new PipelineRegistration(typeof(TMessage), null, Build(builder)));
        services.AddSingleton(provider => provider.GetRequiredService<Pipelines>().Of<TMessage>());
        return services;
    }

    internal static void AddDefaultPipeline<TMessage>(
        this IServiceCollection services, PipelineKind kind, IPipelineBuilder<TMessage> builder)
        where TMessage : class =>
        Register(services, new PipelineRegistration(typeof(TMessage), kind, Build(builder)));

    static Func<IServiceProvider, IPipelineRun> Build<TMessage>(IPipelineBuilder<TMessage> builder)
        where TMessage : class {
        if (builder is null) {
            throw new ArgumentNullException(nameof(builder));
        }
        return provider => builder.Build(provider) as IPipelineRun ?? throw new InvalidOperationException(
            $"{builder.GetType().Name} does not build a VSA pipeline. Use PipelineBuilder<{typeof(TMessage).Name}>.");
    }

    static void Register(IServiceCollection services, PipelineRegistration registration) {
        services.AddSingleton(registration);
        services.TryAddSingleton(provider => new Pipelines(provider, provider.GetServices<PipelineRegistration>()));
    }
}
