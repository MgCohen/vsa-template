using GoodApp.Modules.Notes.Domain.Shared;

namespace GoodApp.Modules.Notes.Slices.ArchiveNote;

public record ArchiveNoteRequest(NoteId Id) : Fw.IRequest;

public class ArchiveNoteHandler : Fw.Handler<ArchiveNoteRequest>
{
    public override void Handle(ArchiveNoteRequest request) { }
}
