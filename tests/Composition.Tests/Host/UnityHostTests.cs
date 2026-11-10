using Cards;
using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Resources;
using Resources.Domain.Shared;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using VSA.Composition;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;

namespace Composition.Tests.Host;

public sealed class UnityHostTests {
    [Test]
    public void The_Unity_host_has_no_logger_providers_and_still_makes_loggers() {
        using var host = App().Build();

        Assert.That(host.Services.GetServices<ILoggerProvider>(), Is.Empty);
        Assert.That(host.Services.GetRequiredService<ILogger<UnityHostTests>>(), Is.Not.Null);
    }

    [Test]
    public void The_Unity_host_has_no_console_lifetime() {
        using var host = App().Build();

        Assert.That(host.Services.GetRequiredService<IHostLifetime>(), Is.Not.InstanceOf<ConsoleLifetime>());
    }

    [Test]
    public void The_Unity_host_content_root_is_the_current_directory() {
        using var host = App().Build();

        Assert.That(host.Services.GetRequiredService<IHostEnvironment>().ContentRootPath,
            Is.EqualTo(Directory.GetCurrentDirectory()).Or.EqualTo(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar));
    }

    [Test]
    public void The_Unity_host_reads_the_configuration_from_args_only() {
        Environment.SetEnvironmentVariable("UnityHostTests__FromEnvironment", "yes");
        try {
            using var host = VsaBuilder.Create("--UnityHostTests:FromArgs=yes").UseUnityHost().Modules().Build();
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            Assert.That(configuration["UnityHostTests:FromArgs"], Is.EqualTo("yes"));
            Assert.That(configuration["UnityHostTests:FromEnvironment"], Is.Null);
        } finally {
            Environment.SetEnvironmentVariable("UnityHostTests__FromEnvironment", null);
        }
    }

    [Test]
    public async Task A_command_goes_through_two_modules_in_the_Unity_host() {
        using var host = App(new CardsModule(), new ResourcesModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default);

        Assert.That(result.Value, Is.EqualTo(new PayCostResult(true, 7)));
    }

    [Test]
    public async Task An_event_reaches_a_live_presenter_in_the_Unity_host() {
        using var host = App(new SignalsModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    [Test]
    public async Task The_Unity_host_runs_until_the_app_stops_it() {
        using var host = App().Build();

        var run = host.RunAsync();
        await host.StopAsync();

        Assert.That(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(10))), Is.SameAs(run));
    }

    static VsaBuilder App(params VSA.Module.Module[] modules) =>
        VsaBuilder.Create().UseUnityHost().Modules(modules);
}
