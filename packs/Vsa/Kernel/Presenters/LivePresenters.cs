namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;

public sealed class LivePresenters(ILogger<LivePresenters> logger) : IIntegrationEventObserver {
    readonly object gate = new();
    readonly HashSet<PresenterScope> scopes = [];

    // The scopes whose gate this async flow holds. A presenter handler can send a request,
    // and the request handler can publish an event to the same scope. That delivery runs
    // in the flow that holds the gate, so it does not wait for the gate again.
    static readonly AsyncLocal<Held?> held = new();

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

    public async Task OnPublishedAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        PresenterScope[] targets;
        lock (gate) {
            targets = [.. scopes];
        }
        foreach (var scope in targets) {
            await DeliverAsync(scope, integrationEvent, ct);
        }
    }

    async Task DeliverAsync<TEvent>(PresenterScope scope, TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        var reentry = Held.Contains(held.Value, scope);
        if (!reentry) {
            await scope.Gate.WaitAsync(CancellationToken.None);
        }
        var outer = held.Value;
        held.Value = new Held(scope, outer);
        try {
            if (!IsLive(scope)) {
                return;
            }
            IPresenterHandler<TEvent>[] handlers;
            try {
                handlers = [.. scope.Services.GetServices<IPresenterHandler<TEvent>>()];
            } catch (Exception e) {
                logger.LogError(e, "The presenters for {Event} failed to resolve.", typeof(TEvent).Name);
                return;
            }
            // Each scope gets its own envelope, with the services of that presenter scope.
            var envelope = Envelope.For(integrationEvent, scope.Services, ct);
            foreach (var handler in handlers) {
                await HandleAsync(handler, envelope);
            }
        } finally {
            held.Value = outer;
            if (!reentry) {
                scope.Gate.Release();
            }
        }
    }

    async Task HandleAsync<TEvent>(IPresenterHandler<TEvent> handler, Envelope<TEvent> envelope) where TEvent : IntegrationEvent {
        try {
            var result = await handler.HandleAsync(envelope);
            if (result.Error is { } error) {
                logger.LogError("{Presenter} failed on {Event}: {Error}", handler.GetType().Name, typeof(TEvent).Name, error.Message);
            }
        } catch (Exception e) {
            logger.LogError(e, "{Presenter} failed on {Event}.", handler.GetType().Name, typeof(TEvent).Name);
        }
        (handler as Presenter)?.RaiseChanged();
    }

    sealed record Held(PresenterScope Scope, Held? Outer) {
        public static bool Contains(Held? held, PresenterScope scope) {
            for (var h = held; h is not null; h = h.Outer) {
                if (ReferenceEquals(h.Scope, scope)) {
                    return true;
                }
            }
            return false;
        }
    }
}
