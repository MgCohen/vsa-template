namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

public static class PresenterServiceCollectionExtensions {
    public static IServiceCollection AddPresenters(this IServiceCollection services, AppTypes types) {
        EnsureEachPresenterIsScoped(types.Implementations(typeof(Presenter)));
        services.AddSingleton<LiveScopes>();
        services.AddScoped<PresenterScope>();
        services.AddPipeline(new PipelineBuilder<IntegrationEvent>()
            .HandlersOf(typeof(IPresenterHandler<>))
            .ValidatorsOf(typeof(PresenterValidator<>))
            .Runner<LiveScopesRunner>());
        return services;
    }

    static void EnsureEachPresenterIsScoped(IEnumerable<ServiceImplementation> presenters) {
        foreach (var presenter in presenters.Select(p => p.Implementation)) {
            if (MarkedTypes.LifetimeOf(presenter) is var lifetime and not ServiceLifetime.Scoped) {
                throw new InvalidOperationException(
                    $"{presenter.FullName} is a presenter with the lifetime {lifetime}. "
                    + "A presenter is scoped: one instance for each presenter scope. "
                    + "Remove the lifetime from its [AutoRegister].");
            }
        }
    }
}
