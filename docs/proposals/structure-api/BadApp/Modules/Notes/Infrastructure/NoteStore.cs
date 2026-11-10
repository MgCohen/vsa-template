using System.Collections.Concurrent;
using BadApp.Modules.Notes.Domain;
using BadApp.Modules.Notes.Domain.Shared;

namespace BadApp.Modules.Notes.Infrastructure;

public sealed class NoteStore
{
    internal ConcurrentDictionary<NoteId, Note> Notes { get; } = new();
}
