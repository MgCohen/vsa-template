using Cards;
using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Echo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Resources;
using Resources.Domain.Shared;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using VSA.Composition;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Contracts.Requests;
using VSA.Presenters;

namespace Composition.Tests;

public sealed class HeadlessCompositionTests {
    [Test]
    public void The_test_app_runs_with_no_AspNetCore_framework() {
        var runtimeConfig = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Composition.Tests.runtimeconfig.json"));

        Assert.That(runtimeConfig, Does.Not.Contain("Microsoft.AspNetCore"));
        Assert.That(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name),
            Has.None.StartsWith("Microsoft.AspNetCore"));
    }

    [Test]
    public async Task A_command_goes_through_two_modules_in_a_plain_service_provider() {
        var services = Services();
        Composer(services, new CardsModule(), new ResourcesModule()).Install();
        await using var provider = Build(services);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default);

        Assert.That(result, Is.EqualTo(new PayCostResult(true, 7)));
    }

    [Test]
    public void A_command_with_two_handlers_stops_the_composition() {
        var composer = Composer(Services(), new EchoModule());

        var error = Assert.Throws<InvalidOperationException>(composer.Install);

        Assert.That(error.Message, Does.StartWith("Command Echo.Slices.Echo.EchoCommand has two handlers:"));
    }

    [Test]
    public async Task An_event_reaches_its_handler_and_a_live_presenter() {
        var services = Services();
        Composer(services, new SignalsModule()).Install();
        await using var provider = Build(services);
        await using var scope = provider.CreateAsyncScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(provider.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    [Test]
    public void The_default_installers_register_no_bus() {
        var services = new ServiceCollection();

        Composer(services, new SignalsModule()).Install();

        Assert.That(services.Any(s => s.ServiceType == typeof(IBus)), Is.False);
    }

    [Test]
    public void A_removed_installer_does_not_run() {
        var services = Services();

        Composer(services, new SignalsModule()).Remove<PresenterInstaller>().Install();

        Assert.That(services.Any(s => s.ServiceType == typeof(BellPresenter)), Is.False);
        Assert.That(services.Any(s => s.ServiceType == typeof(LivePresenters)), Is.False);
    }

    [Test]
    public void An_installer_added_before_the_modules_runs_before_Configure() {
        var services = Services();
        var probe = new RingLogProbe();

        Composer(services, new SignalsModule()).AddBefore<ModuleInstaller>(probe).Install();

        Assert.That(probe.RingLogWasRegistered, Is.False);
        Assert.That(services.Any(s => s.ServiceType == typeof(RingLog)), Is.True);
    }

    [Test]
    public void A_replaced_installer_runs_in_the_same_place() {
        var probe = new RingLogProbe();
        var composer = Composer(Services(), new SignalsModule()).Replace<ModuleInstaller>(probe);

        composer.Install();

        Assert.That(composer.Installers[^1], Is.SameAs(probe));
        Assert.That(probe.RingLogWasRegistered, Is.False);
    }

    [Test]
    public void An_installer_that_the_composer_does_not_have_is_an_error() {
        var composer = Composer(Services()).Remove<PresenterInstaller>();

        var error = Assert.Throws<InvalidOperationException>(() => composer.Remove<PresenterInstaller>());

        Assert.That(error.Message, Is.EqualTo("The composer has no PresenterInstaller."));
    }

    [Test]
    public async Task The_installers_also_run_by_hand_without_the_composer() {
        var services = Services();
        var context = new VsaContext(new MicrosoftServices(services), EmptyConfiguration(), [new CardsModule(), new ResourcesModule()]);
        IVsaInstaller[] installers = [new CoreInstaller(), new CommandHandlerInstaller(), new ModuleInstaller()];
        foreach (var installer in installers) {
            installer.Install(context);
        }
        await using var provider = Build(services);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default);

        Assert.That(result, Is.EqualTo(new PayCostResult(true, 7)));
    }

    static VsaComposer Composer(ServiceCollection services, params VSA.Module.Module[] modules) =>
        new(new MicrosoftServices(services), EmptyConfiguration(), modules);

    static ServiceCollection Services() {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IBus>(new NoRequestsBus());
        return services;
    }

    static ServiceProvider Build(ServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    sealed class RingLogProbe : IVsaInstaller {
        public bool? RingLogWasRegistered { get; private set; }

        public void Install(VsaContext context) => RingLogWasRegistered = context.Services.Contains(typeof(RingLog));
    }

    sealed class NoRequestsBus : IBus {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            where TResponse : IResponse =>
            throw new InvalidOperationException("This test app has no request handlers.");
    }
}
