using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using VSA.Composition;

namespace Composition.Tests.Host;

public sealed class GenericVsaHostLoggingTests {
    [Test]
    public void A_host_with_no_event_log_has_no_EventLog_logger_provider_and_makes_loggers() {
        using var host = Build(new GenericVsaHost(eventLog: false));

        Assert.That(ProviderTypes(host), Does.Not.Contain(typeof(EventLogLoggerProvider)));
        Assert.That(host.Services.GetRequiredService<ILogger<GenericVsaHostLoggingTests>>(), Is.Not.Null);
    }

    [Test]
    public void The_net10_build_keeps_the_EventLog_logger_provider_on_Windows() {
        Assume.That(OperatingSystem.IsWindows(), "The default host adds the EventLog provider only on Windows.");
        using var host = Build(new GenericVsaHost());

        Assert.That(GenericVsaHost.PlatformHasEventLog, Is.True);
        Assert.That(ProviderTypes(host), Does.Contain(typeof(EventLogLoggerProvider)));
    }

    static IEnumerable<Type> ProviderTypes(IHost host) {
        return host.Services.GetServices<ILoggerProvider>().Select(provider => provider.GetType());
    }

    static IHost Build(GenericVsaHost vsaHost) {
        var builder = vsaHost.CreateBuilder([]);
        return vsaHost.Build(builder);
    }
}
