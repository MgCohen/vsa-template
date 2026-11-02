using System.Linq.Expressions;
using VSA.Contracts.Events;
using VSA.Domain;
using VSA.Infrastructure;

namespace VSA.Testing.Unit;

// The default repository in unit tests. Add and Remove take effect on SaveChangesAsync, as with EF Core.
public sealed class InMemoryRepository<T, TId> : IRepository<T, TId> where T : AggregateRoot<TId> where TId : notnull {
    readonly Dictionary<TId, T> saved = [];
    readonly List<(T Aggregate, bool Add)> pending = [];

    public IReadOnlyCollection<T> Saved => saved.Values;
    public int SaveCount { get; private set; }

    // Puts aggregates in the store before the test acts, with no save.
    public InMemoryRepository<T, TId> With(params T[] aggregates) {
        foreach (var aggregate in aggregates) {
            saved[aggregate.Id] = aggregate;
        }
        return this;
    }

    public Task<T?> GetAsync(TId id, CancellationToken ct) => Task.FromResult(saved.GetValueOrDefault(id));

    public Task<List<T>> ListAsync(Expression<Func<T, bool>> where, CancellationToken ct) =>
        Task.FromResult(saved.Values.Where(where.Compile()).ToList());

    public void Add(T aggregate) => pending.Add((aggregate, true));

    public void Remove(T aggregate) => pending.Add((aggregate, false));

    public Task SaveChangesAsync(CancellationToken ct) {
        foreach (var (aggregate, add) in pending) {
            if (add) {
                saved[aggregate.Id] = aggregate;
            } else {
                saved.Remove(aggregate.Id);
            }
        }
        pending.Clear();
        SaveCount++;
        return Task.CompletedTask;
    }
}

// The default event publisher in unit tests. It keeps each event and sends nothing.
public sealed class RecordingEventPublisher : IIntegrationEventPublisher {
    readonly List<IntegrationEvent> published = [];

    public IReadOnlyList<IntegrationEvent> Published => published;

    public IEnumerable<TEvent> OfType<TEvent>() where TEvent : IntegrationEvent => published.OfType<TEvent>();

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        published.Add(integrationEvent);
        return Task.CompletedTask;
    }
}
