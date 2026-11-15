using Umpire.Vsa.Domain;

namespace Umpire.Vsa.Infrastructure;

public interface IRepository<T, TId> where T : Entity<TId> where TId : notnull {
    Task<T?> GetAsync(TId id, CancellationToken ct);
    Task<List<T>> ListAsync(Func<T, bool> where, CancellationToken ct);
    void Add(T entity);
    void Remove(T entity);
    Task SaveChangesAsync(CancellationToken ct);
}
