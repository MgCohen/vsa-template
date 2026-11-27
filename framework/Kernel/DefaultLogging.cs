namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

static class DefaultLogging {
    internal static void AddIfMissing(IServiceCollection services) {
        var hasFactory = services.Any(service => service.ServiceType == typeof(ILoggerFactory));
        if (!hasFactory && services.Any(service => service.ServiceType == typeof(ILoggerProvider))) {
            throw new InvalidOperationException(
                "A service registers an ILoggerProvider, but no service registers an ILoggerFactory, "
                + "so the provider gets no log. Call services.AddLogging() or register an ILoggerFactory.");
        }
        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
    }
}
