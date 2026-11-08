using VSA.Domain;

namespace VSA.Infrastructure;

// The persistence port. Each module writes its adapter in Infrastructure/. Unit tests use InMemoryRepository.
public interface IRepository<T, TId> where T : AggregateRoot<TId> where TId : notnull {
    Task<T?> GetAsync(TId id, CancellationToken ct);
    Task<List<T>> ListAsync(Func<T, bool> where, CancellationToken ct);
    void Add(T aggregate);
    void Remove(T aggregate);
    Task SaveChangesAsync(CancellationToken ct);
}
