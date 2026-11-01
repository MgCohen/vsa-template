using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VSA.Domain;

namespace VSA.Infrastructure.EfCore;

// The default for every aggregate. All methods are virtual, so a custom repository can change one.
public class EfRepository<T, TId>(ModuleDbContext db) : IRepository<T, TId>
    where T : AggregateRoot<TId> where TId : notnull {
    protected ModuleDbContext Db => db;

    public virtual async Task<T?> GetAsync(TId id, CancellationToken ct) =>
        await db.Set<T>().FindAsync([id], ct);

    public virtual Task<List<T>> ListAsync(Expression<Func<T, bool>> where, CancellationToken ct) =>
        db.Set<T>().Where(where).ToListAsync(ct);

    public virtual void Add(T aggregate) => db.Set<T>().Add(aggregate);

    public virtual void Remove(T aggregate) => db.Set<T>().Remove(aggregate);

    public virtual Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
