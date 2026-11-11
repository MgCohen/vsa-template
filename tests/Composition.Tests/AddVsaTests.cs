using Cards;
using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Resources;
using Resources.Domain.Shared;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using VSA.Composition;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;

namespace Composition.Tests;

public sealed class AddVsaTests {
    [Test]
    public async Task A_command_goes_through_two_modules_with_no_host() {
        await using var provider = Compose(new CardsModule(), new ResourcesModule());
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default);

        Assert.That(result.Value, Is.EqualTo(new PayCostResult(true, 7)));
    }

    [Test]
    public async Task An_event_reaches_its_handler_and_a_live_presenter_with_no_host() {
        await using var provider = Compose(new SignalsModule());
        await using var scope = provider.CreateAsyncScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(provider.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    [Test]
    public async Task AddVsa_with_no_configuration_reads_the_args() {
        await using var provider = new ServiceCollection()
            .AddVsa(["--Compose:FromArgs=yes"], vsa => vsa.Modules()).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IConfiguration>()["Compose:FromArgs"], Is.EqualTo("yes"));
    }

    [Test]
    public async Task AddVsa_uses_the_configuration_that_the_app_registered_first() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Compose:FromApp"] = "yes" }).Build();

        await using var provider = new ServiceCollection().AddSingleton<IConfiguration>(configuration)
            .AddVsa(["--Compose:FromArgs=yes"], vsa => vsa.Modules()).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IConfiguration>(), Is.SameAs(configuration));
    }

    [Test]
    public void AddVsa_adds_no_host_parts_and_no_logger_providers() {
        var services = new ServiceCollection().AddVsa([], vsa => vsa.Modules());

        Assert.That(services.Select(service => service.ServiceType), Has.None.EqualTo(typeof(IHostLifetime))
            .And.None.EqualTo(typeof(IHostedService))
            .And.None.EqualTo(typeof(ILoggerProvider))
            .And.Some.EqualTo(typeof(ILoggerFactory)));
    }

    [Test]
    public async Task App_services_come_before_the_modules_with_no_host() {
        var appRingLog = new RingLog();

        await using var provider = new ServiceCollection()
            .AddVsa([], vsa => vsa.Modules(new SignalsModule()).Services(services => services.AddSingleton(appRingLog)))
            .BuildServiceProvider();

        Assert.That(provider.GetRequiredService<RingLog>(), Is.Not.SameAs(appRingLog));
    }

    static ServiceProvider Compose(params VSA.Module.Module[] modules) =>
        new ServiceCollection().AddVsa([], vsa => vsa.Modules(modules)).BuildServiceProvider();
}
