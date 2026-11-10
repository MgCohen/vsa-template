using GoodApp.Modules.Notes.Domain;
using GoodApp.Modules.Notes.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace GoodApp.Modules.Notes.Slices.ArchiveNote;

sealed class ArchiveNoteHandler(IRepository<Note, NoteId> notes) : RequestHandler<ArchiveNoteRequest, ArchiveNoteResponse>
{
    static readonly Error NotFound = new("notes.note_not_found", "The note does not exist.");

    public override async Task<Result<ArchiveNoteResponse>> HandleAsync(Envelope<ArchiveNoteRequest> envelope)
    {
        var note = await notes.GetAsync(new NoteId(envelope.Message.NoteId), envelope.Cancellation);
        if (note is null) return NotFound;
        note.Archive();
        await notes.SaveChangesAsync(envelope.Cancellation);
        return new ArchiveNoteResponse(note.Archived);
    }
}
