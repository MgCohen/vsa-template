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

public sealed class PresenterCallTests {
    [Test]
    public async Task CallsPresentersTwoTimesRaisesChangedOneTime() {
        using var services = Services(new PipelineBuilder<IntegrationEvent>()
            .Handlers(typeof(BeepPresenter)).CallsPresenters().CallsPresenters().Runner<LiveScopesRunner>());
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        var presenter = scope.ServiceProvider.GetRequiredService<BeepPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await services.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Beeped(), CancellationToken.None);

        Assert.That(presenter.Beeps, Is.EqualTo(1));
        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task TheEventPipelineDoesNotCallAPresenter() {
        using var services = Services(null);
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        var presenter = scope.ServiceProvider.GetRequiredService<BeepPresenter>();

        var result = await services.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Beeped(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(presenter.Beeps, Is.Zero);
    }

    static ServiceProvider Services(PipelineBuilder<IntegrationEvent>? presenters) {
        var services = new ServiceCollection();
        services.AddSingleton(new AppTypes(new FixedModules(), typeof(PresenterCallTests).Assembly));
        services.AddSingleton<ILogger<LiveScopesRunner>>(NullLogger<LiveScopesRunner>.Instance);
        services.AddSingleton<LiveScopes>();
        services.AddScoped<PresenterScope>();
        services.AddScoped<BeepPresenter>();
        services.AddEventHandlers();
        if (presenters is not null) {
            services.AddPipeline(presenters);
        }
        return services.BuildServiceProvider();
    }

    public sealed record Beeped : IntegrationEvent;

    public sealed class BeepPresenter() : Presenter(null!), IIntegrationEventHandler<Beeped> {
        public int Beeps { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Beeped> envelope) {
            Beeps++;
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }
}
