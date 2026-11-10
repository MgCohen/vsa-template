using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using VSA.Module;

namespace VSA.Composition;

sealed class GenericVsaHost : IVsaHost {
#if NETSTANDARD2_1
    internal const bool PlatformHasEventLog = false;
#else
    internal const bool PlatformHasEventLog = true;
#endif

    readonly bool eventLog;

    public GenericVsaHost(bool eventLog = PlatformHasEventLog) {
        this.eventLog = eventLog;
    }

    public IHostApplicationBuilder CreateBuilder(string[] args) {
        var builder = Host.CreateApplicationBuilder(args);
        if (!eventLog) {
            RemoveEventLogProvider(builder.Logging.Services);
        }
        return builder;
    }

    public void Install(IHostApplicationBuilder builder, ModuleTypes types) {
    }

    public IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();

    static void RemoveEventLogProvider(IServiceCollection services) {
        var eventLogProviders = services
            .Where(service => service.ServiceType == typeof(ILoggerProvider)
                && service.ImplementationType == typeof(EventLogLoggerProvider))
            .ToList();
        foreach (var provider in eventLogProviders) {
            services.Remove(provider);
        }
    }
}
