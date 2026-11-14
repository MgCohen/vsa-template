using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VSA.Kernel;

namespace VSA.Handlers;

sealed class PipelineInstaller : IInstaller {
    public void Install(IServiceCollection services, AnchorTypes types) {
        var catalog = new PipelineCatalog(types);
        services.AddSingleton(catalog);
        services.TryAddSingleton<IMessageIds, SequentialMessageIds>();
        services.TryAddSingleton(provider => new Pipelines(provider, provider.GetServices<PipelineRegistration>()));
    }
}
