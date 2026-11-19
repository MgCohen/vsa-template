namespace Composition.Tests;

using Cards;
using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Resources;
using Resources.Domain.Shared;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Presenters;

public sealed class ComposeTests {
    [Test]
    public async Task A_command_goes_through_two_modules_with_no_host() {
        await using var provider = Compose(new CardsModule(), new ResourcesModule());
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana.Name, 3)), default);

        Assert.That(result.Value, Is.EqualTo(new PayCostResult(true, 7)));
    }

    [Test]
    public async Task An_event_reaches_its_handler_and_a_live_presenter_with_no_host() {
        await using var provider = Compose(new SignalsModule());
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(provider.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    [Test]
    public async Task Compose_with_no_configuration_adds_an_empty_configuration() {
        await using var provider = new TestApp().Compose(new ServiceCollection()).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IConfiguration>().AsEnumerable(), Is.Empty);
    }

    [Test]
    public async Task Compose_keeps_the_configuration_that_the_services_have() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Compose:FromApp"] = "yes" }).Build();

        await using var provider = new TestApp()
            .Compose(new ServiceCollection().AddSingleton<IConfiguration>(configuration)).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IConfiguration>(), Is.SameAs(configuration));
    }

    [Test]
    public void Compose_adds_no_host_parts_and_no_logger_providers() {
        var services = new TestApp().Compose(new ServiceCollection());

        Assert.That(services.Select(service => service.ServiceType.Namespace), Has.None.EqualTo("Microsoft.Extensions.Hosting"));
        Assert.That(services.Select(service => service.ServiceType), Has.None.EqualTo(typeof(ILoggerProvider))
            .And.Some.EqualTo(typeof(ILoggerFactory)));
    }

    [Test]
    public async Task The_host_services_come_after_the_modules_and_win() {
        var appRingLog = new RingLog();

        await using var provider = new TestApp(new SignalsModule()).With(services => services.AddSingleton(appRingLog))
            .Compose(new ServiceCollection()).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<RingLog>(), Is.SameAs(appRingLog));
    }

    static ServiceProvider Compose(params Umpire.Kernels.Module[] modules) =>
        new TestApp(modules).Compose(new ServiceCollection()).BuildServiceProvider();
}
