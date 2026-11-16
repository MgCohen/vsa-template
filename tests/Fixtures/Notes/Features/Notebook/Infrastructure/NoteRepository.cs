namespace Notes.Features.Notebook.Infrastructure;

using Notes.Features.Notebook.Domain;
using Umpire.Vsa.Infrastructure;

public sealed class NoteRepository(NoteStore store) : IRepository<Note, NoteId> {
    readonly List<PendingChange> pending = [];

    public Task<Note?> GetAsync(NoteId id, CancellationToken ct) =>
        Task.FromResult(store.Notes.GetValueOrDefault(id));

    public Task<List<Note>> ListAsync(Func<Note, bool> where, CancellationToken ct) =>
        Task.FromResult(store.Notes.Values.Where(where).ToList());

    public void Add(Note entity) => pending.Add(new PendingChange(entity, true));

    public void Remove(Note entity) => pending.Add(new PendingChange(entity, false));

    public Task SaveChangesAsync(CancellationToken ct) {
        foreach (var change in pending) {
            if (change.IsAdd) {
                store.Notes[change.Entity.Id] = change.Entity;
            } else {
                store.Notes.TryRemove(change.Entity.Id, out _);
            }
        }
        pending.Clear();
        return Task.CompletedTask;
    }

    readonly record struct PendingChange(Note Entity, bool IsAdd);
}
