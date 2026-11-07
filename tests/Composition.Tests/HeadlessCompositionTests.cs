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
        var runtimeConfig = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Composition.Tests.runtimeconfig.json"));

        Assert.That(runtimeConfig, Does.Not.Contain("Microsoft.AspNetCore"));
        Assert.That(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name),
            Has.None.StartsWith("Microsoft.AspNetCore"));
    }

    [Test]
    public async Task A_command_goes_through_two_modules_in_a_plain_service_provider() {
        await using var provider = Build(new CardsModule(), new ResourcesModule());
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default);

        Assert.That(result, Is.EqualTo(new PayCostResult(true, 7)));
    }

    [Test]
    public void A_command_with_two_handlers_stops_the_composition() {
        var services = new ServiceCollection();

        var error = Assert.Throws<InvalidOperationException>(() => services.AddVsa(EmptyConfiguration(), new EchoModule()));

        Assert.That(error.Message, Does.StartWith("Command Echo.Slices.Echo.EchoCommand has two handlers:"));
    }

    [Test]
    public async Task An_event_reaches_its_handler_and_a_live_presenter() {
        await using var provider = Build(new SignalsModule());
        await using var scope = provider.CreateAsyncScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(provider.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    [Test]
    public void The_composition_registers_no_bus() {
        var services = new ServiceCollection();

        services.AddVsa(EmptyConfiguration(), new SignalsModule());

        Assert.That(services.Any(s => s.ServiceType == typeof(IBus)), Is.False);
    }

    static ServiceProvider Build(params VSA.Module.Module[] modules) {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IBus, NoRequestsBus>();
        services.AddVsa(EmptyConfiguration(), modules);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    sealed class NoRequestsBus : IBus {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            where TResponse : IResponse =>
            throw new InvalidOperationException("This test app has no request handlers.");
    }
}
