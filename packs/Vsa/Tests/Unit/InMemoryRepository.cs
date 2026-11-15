using Umpire.Vsa.Domain;
using Umpire.Vsa.Infrastructure;

namespace Umpire.Vsa.Testing.Unit;

public sealed class InMemoryRepository<T, TId> : IRepository<T, TId> where T : Entity<TId> where TId : notnull {
    readonly Dictionary<TId, T> saved = [];
    readonly List<PendingChange> pending = [];

    public IReadOnlyCollection<T> Saved => saved.Values;
    public int SaveCount { get; private set; }

    public InMemoryRepository<T, TId> With(params T[] entities) {
        foreach (var entity in entities) {
            saved[entity.Id] = entity;
        }
        return this;
    }

    public Task<T?> GetAsync(TId id, CancellationToken ct) => Task.FromResult(saved.GetValueOrDefault(id));

    public Task<List<T>> ListAsync(Func<T, bool> where, CancellationToken ct) =>
        Task.FromResult(saved.Values.Where(where).ToList());

    public void Add(T entity) => pending.Add(new PendingChange(entity, true));

    public void Remove(T entity) => pending.Add(new PendingChange(entity, false));

    public Task SaveChangesAsync(CancellationToken ct) {
        foreach (var change in pending) {
            if (change.IsAdd) {
                saved[change.Entity.Id] = change.Entity;
            } else {
                saved.Remove(change.Entity.Id);
            }
        }
        pending.Clear();
        SaveCount++;
        return Task.CompletedTask;
    }

    readonly record struct PendingChange(T Entity, bool IsAdd);
}
