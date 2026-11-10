using BadApp.Modules.Billing.Domain;
using BadApp.Modules.Notes.Domain;

namespace BadApp.Modules.Notes.Slices.CreateNote;

public record CreateNoteRequest(string Text) : Fw.IRequest;

public class CreateNoteHandler : Fw.Handler<CreateNoteRequest>
{
    public override void Handle(CreateNoteRequest request) => _ = new Note(new ArchiveNote.ArchiveNoteRequest(default).ToString() + request.Text);
    public Invoice? Linked { get; set; }
}
