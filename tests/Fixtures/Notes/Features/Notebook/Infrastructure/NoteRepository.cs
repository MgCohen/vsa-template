namespace Notes.Features.Notebook.Infrastructure;

using System.Collections.Concurrent;
using Notes.Features.Notebook.Domain;
using Umpire.Vsa.Infrastructure;

// The saved notes of the app. The sample has no database, so the notes stay in memory while the app runs.
public sealed class NoteStore {
    internal ConcurrentDictionary<NoteId, Note> Notes { get; } = new();
}

// One repository for each request scope. Add and Remove take effect on SaveChangesAsync.
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
