using Microsoft.Extensions.DependencyInjection;

namespace VSA.Module;

sealed class MicrosoftResolver(IServiceProvider provider) : IVsaResolver {
    public object? Resolve(Type service) => provider.GetService(service);

    public IEnumerable<object> ResolveAll(Type service) => provider.GetServices(service).OfType<object>();

    public object Create(Type type) => ActivatorUtilities.CreateInstance(provider, type);
}
