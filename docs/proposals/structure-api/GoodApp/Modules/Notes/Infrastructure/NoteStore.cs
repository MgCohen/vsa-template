using System.Collections.Concurrent;
using GoodApp.Modules.Notes.Domain;
using GoodApp.Modules.Notes.Domain.Shared;

namespace GoodApp.Modules.Notes.Infrastructure;

public sealed class NoteStore
{
    internal ConcurrentDictionary<NoteId, Note> Notes { get; } = new();
}
