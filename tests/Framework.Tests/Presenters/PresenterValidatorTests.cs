namespace Framework.Tests.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Presenters;

public sealed class PresenterValidatorTests {
    [Test]
    public async Task TheEventValidatorRunsOneTimeAndThePresenterValidatorRunsInEachLiveScope() {
        using var services = PresenterServices();
        using var first = LiveScope(services);
        using var second = LiveScope(services);

        await Publish(services, new Tallied(2), CancellationToken.None);

        var runs = services.GetRequiredService<ValidatorRuns>();
        Assert.That(runs.Event, Is.EqualTo(1));
        Assert.That(runs.Presenter, Is.EqualTo(2));
        Assert.That(first.ServiceProvider.GetRequiredService<TallyPresenter>().Total, Is.EqualTo(2));
        Assert.That(second.ServiceProvider.GetRequiredService<TallyPresenter>().Total, Is.EqualTo(2));
    }

    [Test]
    public async Task APresenterValidatorOfAnInterfaceKeepsTheEventFromThePresenters() {
        using var services = PresenterServices();
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<TallyPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        var result = await Publish(services, new Tallied(-1), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(presenter.Total, Is.Zero);
        Assert.That(changed, Is.Zero);
    }

    static ServiceProvider PresenterServices() {
        var services = new ServiceCollection();
        var types = new AppTypes(new FixedModules(), typeof(PresenterValidatorTests).Assembly);
        services.AddSingleton(types);
        services.AddSingleton<ILogger<LiveScopesRunner>>(NullLogger<LiveScopesRunner>.Instance);
        services.AddSingleton<ValidatorRuns>();
        services.AddScoped<TallyPresenter>();
        services.AddEventHandlers();
        services.AddPresenters(types);
        return services.BuildServiceProvider();
    }

    static IServiceScope LiveScope(IServiceProvider services) {
        var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        return scope;
    }

    static Task<Result<Unit>> Publish(IServiceProvider services, IntegrationEvent published, CancellationToken ct) =>
        services.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(published, ct);

    public interface ITally {
        int By { get; }
    }

    public sealed record Tallied(int By) : IntegrationEvent, ITally;

    public sealed class ValidatorRuns {
        int eventRuns;
        int presenterRuns;

        public int Event => eventRuns;
        public int Presenter => presenterRuns;

        public void CountEvent() => Interlocked.Increment(ref eventRuns);

        public void CountPresenter() => Interlocked.Increment(ref presenterRuns);
    }

    public sealed class TalliedValidator(ValidatorRuns runs) : Validator<Tallied> {
        public override Error? Validate(IEnvelope<Tallied> envelope) {
            runs.CountEvent();
            return null;
        }
    }

    public sealed class TallyCheck(ValidatorRuns runs) : PresenterValidator<ITally> {
        public override Error? Validate(IEnvelope<ITally> envelope) {
            runs.CountPresenter();
            return envelope.Message.By < 0 ? new Error("tally.negative", "A tally has no negative value.") : null;
        }
    }

    public sealed class TallyPresenter() : Presenter(null!), IIntegrationEventHandler<Tallied> {
        public int Total { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Tallied> envelope) {
            Total += envelope.Message.By;
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }
}
