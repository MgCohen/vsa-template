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
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Module;
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
    public void The_composer_registers_no_bus() {
        var services = new ServiceCollection();

        Composer(services, new SignalsModule()).Install();

        Assert.That(services.Any(s => s.ServiceType == typeof(IBus)), Is.False);
    }

    [Test]
    public async Task A_module_registration_wins_over_app_code_before_Install() {
        var services = Services();
        var appRingLog = new RingLog();
        services.AddSingleton(appRingLog);

        Composer(services, new SignalsModule()).Install();
        await using var provider = Build(services);

        Assert.That(provider.GetRequiredService<RingLog>(), Is.Not.SameAs(appRingLog));
    }

    [Test]
    public void Implementations_finds_the_handlers_and_not_the_helper_classes() {
        var context = new VsaContext(Services(), EmptyConfiguration(), [new SignalsModule()]);

        var handlers = context.Implementations(typeof(VSA.Handlers.Events.IntegrationEventHandler<>)).ToList();
        var presenters = context.Implementations(typeof(Presenter)).Select(p => p.Implementation).ToList();

        Assert.That(handlers.Select(h => h.Service), Is.EqualTo(new[] { typeof(VSA.Handlers.Events.IntegrationEventHandler<Rang>) }));
        Assert.That(handlers.Select(h => h.Implementation.Name), Is.EqualTo(new[] { "RangHandler" }));
        Assert.That(presenters, Is.EqualTo(new[] { typeof(BellPresenter) }));
    }

    [Test]
    public async Task The_installers_also_run_by_hand_without_the_composer() {
        var services = Services();
        var context = new VsaContext(services, EmptyConfiguration(), [new CardsModule(), new ResourcesModule()]);
        EventHandlerInstaller.Install(context);
        CommandHandlerInstaller.Install(context);
        ModuleInstaller.Install(context);
        await using var provider = Build(services);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default);

        Assert.That(result, Is.EqualTo(new PayCostResult(true, 7)));
    }

    static VsaComposer Composer(ServiceCollection services, params VSA.Module.Module[] modules) =>
        new(services, EmptyConfiguration(), modules);

    static ServiceCollection Services() {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IBus>(new NoRequestsBus());
        return services;
    }

    static ServiceProvider Build(ServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    sealed class NoRequestsBus : IBus {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            where TResponse : IResponse =>
            throw new InvalidOperationException("This test app has no request handlers.");
    }
}
