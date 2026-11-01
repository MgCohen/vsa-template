using System.Linq.Expressions;
using VSA.Domain;

namespace VSA.Infrastructure;

// An interface: EF Core by default, a fake in tests.
public interface IRepository<T, TId> where T : AggregateRoot<TId> where TId : notnull {
    Task<T?> GetAsync(TId id, CancellationToken ct);
    Task<List<T>> ListAsync(Expression<Func<T, bool>> where, CancellationToken ct);
    void Add(T aggregate);
    void Remove(T aggregate);
    Task SaveChangesAsync(CancellationToken ct);
}
