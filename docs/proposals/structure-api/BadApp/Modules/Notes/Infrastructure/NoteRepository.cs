using BadApp.Modules.Notes.Domain;
using BadApp.Modules.Notes.Domain.Shared;
using VSA.Infrastructure;

namespace BadApp.Modules.Notes.Infrastructure;

public sealed class NoteRepository(NoteStore store) : IRepository<Note, NoteId>
{
    public Task<Note?> GetAsync(NoteId id, CancellationToken ct) => Task.FromResult(store.Notes.GetValueOrDefault(id));

    public Task<List<Note>> ListAsync(Func<Note, bool> where, CancellationToken ct) => Task.FromResult(store.Notes.Values.Where(where).ToList());

    public void Add(Note aggregate) => store.Notes[aggregate.Id] = aggregate;

    public void Remove(Note aggregate) => store.Notes.TryRemove(aggregate.Id, out _);

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}
