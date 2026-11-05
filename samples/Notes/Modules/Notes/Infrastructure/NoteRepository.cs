using System.Collections.Concurrent;
using Notes.Domain;
using VSA.Infrastructure;

namespace Notes.Infrastructure;

// The saved notes of the app. The sample has no database, so the notes stay in memory while the app runs.
public sealed class NoteStore {
    internal ConcurrentDictionary<NoteId, Note> Notes { get; } = new();
}

// One repository for each request scope. Add and Remove take effect on SaveChangesAsync.
public sealed class NoteRepository(NoteStore store) : IRepository<Note, NoteId> {
    readonly List<(Note Note, bool Add)> pending = [];

    public Task<Note?> GetAsync(NoteId id, CancellationToken ct) =>
        Task.FromResult(store.Notes.GetValueOrDefault(id));

    public Task<List<Note>> ListAsync(Func<Note, bool> where, CancellationToken ct) =>
        Task.FromResult(store.Notes.Values.Where(where).ToList());

    public void Add(Note aggregate) => pending.Add((aggregate, true));

    public void Remove(Note aggregate) => pending.Add((aggregate, false));

    public Task SaveChangesAsync(CancellationToken ct) {
        foreach (var (note, add) in pending) {
            if (add) {
                store.Notes[note.Id] = note;
            } else {
                store.Notes.TryRemove(note.Id, out _);
            }
        }
        pending.Clear();
        return Task.CompletedTask;
    }
}
