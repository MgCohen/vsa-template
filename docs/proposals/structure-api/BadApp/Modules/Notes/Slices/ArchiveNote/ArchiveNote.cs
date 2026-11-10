using BadApp.Modules.Notes.Domain.Shared;

namespace BadApp.Modules.Notes.Slices.ArchiveNote;

public record ArchiveNoteRequest(NoteId Id) : Fw.IRequest;

public class ArchiveNoteHandler : Fw.Handler<ArchiveNoteRequest>
{
    public override void Handle(ArchiveNoteRequest request) { }
}
