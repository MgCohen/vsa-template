using GoodApp.Modules.Billing.Domain.Shared;
using GoodApp.Modules.Notes.Domain;

namespace GoodApp.Modules.Notes.Slices.CreateNote;

public record CreateNoteRequest(string Text, InvoiceId? Invoice) : Fw.IRequest;

public class CreateNoteHandler : Fw.Handler<CreateNoteRequest>
{
    public override void Handle(CreateNoteRequest request) => _ = new Note(request.Text);
}
