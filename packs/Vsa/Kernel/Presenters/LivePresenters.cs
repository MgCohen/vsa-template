namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;

public sealed class LivePresenters(ILogger<LivePresenters> logger, IServiceProvider rootServices) : IIntegrationEventObserver {
    readonly object gate = new();
    readonly HashSet<PresenterScope> scopes = [];

    internal IServiceProvider RootServices => rootServices;

    public async Task OnPublishedAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        PresenterScope[] targets;
        lock (gate) {
            targets = [.. scopes];
        }
        foreach (var scope in targets) {
            await scope.DeliverInTurnAsync(() => DeliverAsync(scope, integrationEvent, ct));
        }
    }

    internal void Add(PresenterScope scope) {
        lock (gate) {
            scopes.Add(scope);
        }
    }

    internal void Remove(PresenterScope scope) {
        lock (gate) {
            scopes.Remove(scope);
        }
    }

    bool IsLive(PresenterScope scope) {
        lock (gate) {
            return scopes.Contains(scope);
        }
    }

    async Task DeliverAsync<TEvent>(PresenterScope scope, TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        if (!IsLive(scope)) {
            return;
        }
        IPresenterHandler<TEvent>[] handlers;
        try {
            handlers = [.. scope.Services.GetServices<IPresenterHandler<TEvent>>()];
        } catch (ObjectDisposedException) when (!IsLive(scope)) {
            return;
        } catch (Exception e) {
            logger.LogError(e, "The presenters for {Event} failed to resolve.", typeof(TEvent).Name);
            return;
        }
        var envelopeOfThisScope = Envelope.For(integrationEvent, scope.Services, ct);
        foreach (var handler in handlers) {
            if (!IsLive(scope)) {
                return;
            }
            await HandleAsync(scope, handler, envelopeOfThisScope);
        }
    }

    async Task HandleAsync<TEvent>(PresenterScope scope, IPresenterHandler<TEvent> handler, Envelope<TEvent> envelope)
        where TEvent : IntegrationEvent {
        try {
            var result = await handler.HandleAsync(envelope);
            if (result.Error is { } error) {
                logger.LogError("{Presenter} failed on {Event}: {Error}", handler.GetType().Name, typeof(TEvent).Name, error.Message);
            }
        } catch (ObjectDisposedException) when (!IsLive(scope)) {
            return;
        } catch (OperationCanceledException) when (envelope.Cancellation.IsCancellationRequested) {
            return;
        } catch (Exception e) {
            logger.LogError(e, "{Presenter} failed on {Event}.", handler.GetType().Name, typeof(TEvent).Name);
        }
        RaiseChanged(handler);
    }

    void RaiseChanged(object handler) {
        try {
            (handler as Presenter)?.RaiseChanged();
        } catch (Exception e) {
            logger.LogError(e, "A Changed listener of {Presenter} failed.", handler.GetType().Name);
        }
    }
}
