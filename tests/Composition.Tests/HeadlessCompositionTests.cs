using Answers;
using Answers.Slices.Answer;
using Answers.Slices.GetItem;
using Answers.Slices.ListAll;
using Cards;
using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Echo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Resources;
using Resources.Domain.Shared;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using VSA.Composition;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
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
    public async Task A_command_goes_through_two_modules_in_a_generic_host() {
        using var host = App(new CardsModule(), new ResourcesModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana.Name, 3)), default);

        Assert.That(result.Value, Is.EqualTo(new PayCostResult(true, 7)));
    }

    [Test]
    public void A_command_with_two_handlers_stops_the_build() {
        var app = App(new EchoModule());

        var error = Assert.Throws<InvalidOperationException>(() => app.Build());

        Assert.That(error.Message, Does.StartWith("Command Echo.Slices.Echo.EchoCommand has two handlers:"));
    }

    [Test]
    public async Task An_event_reaches_its_handler_and_a_live_presenter() {
        using var host = App(new SignalsModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(host.Services.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    [Test]
    public async Task A_request_goes_to_its_handler_in_process_with_no_route() {
        using var host = App(new AnswersModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var id = Guid.NewGuid();

        var response = await scope.ServiceProvider.GetRequiredService<IBus>().Send(new GetItemRequest(id, 2, false));

        Assert.That(response.Value, Is.EqualTo(new GetItemResponse(id, 2, false)));
    }

    [Test]
    public async Task A_request_error_comes_back_in_the_result() {
        using var host = App(new AnswersModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var result = await bus.Send(new AnswerRequest("answers.not_found"));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo(new Error("answers.not_found", "The test error.")));
        Assert.That(result.Envelope?.State, Is.EqualTo(EnvelopeState.Accepted));
    }

    [Test]
    public async Task A_request_with_no_handler_is_an_error() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => bus.Send(new ListAllRequest()));

        Assert.That(error!.Message, Does.StartWith("Answers.Slices.ListAll.ListAllRequest has no handler"));
    }

    [Test]
    public void A_module_registration_wins_over_app_services() {
        var appRingLog = new RingLog();

        using var host = App(new SignalsModule()).Services(services => services.AddSingleton(appRingLog)).Build();

        Assert.That(host.Services.GetRequiredService<RingLog>(), Is.Not.SameAs(appRingLog));
    }

    [Test]
    public void Build_with_no_host_is_an_error() {
        var app = VsaBuilder.Create().Modules();

        var error = Assert.Throws<InvalidOperationException>(() => app.Build());

        Assert.That(error.Message, Is.EqualTo(
            "The app has no host. Select one before Build: UseWebHost() or UseGenericHost()."));
    }

    [Test]
    public async Task The_generic_host_runs_until_the_app_stops_it() {
        using var host = App().Build();

        var run = host.RunAsync();
        await host.StopAsync();

        Assert.That(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(10))), Is.SameAs(run));
    }

    static VsaBuilder App(params VSA.Module.Module[] modules) =>
        VsaBuilder.Create().UseGenericHost().Modules(modules);
}
