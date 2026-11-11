using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VSA.Module;

namespace VSA.Handlers;

static class PipelineInstaller {
    internal static void Install(IServiceCollection services, ModuleTypes types) {
        var catalog = new PipelineCatalog(types);
        services.AddSingleton(catalog);
        services.TryAddSingleton<IMessageIds, SequentialMessageIds>();
        services.TryAddSingleton(provider => new Pipelines(provider, provider.GetServices<PipelineRegistration>()));
    }
}
