namespace Notes.Features.Notebook.Infrastructure;

using System.Collections.Concurrent;
using Notes.Features.Notebook.Domain;

public sealed class NoteStore {
    internal ConcurrentDictionary<NoteId, Note> Notes { get; } = new();
}
