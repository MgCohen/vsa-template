using GoodApp.Modules.Billing.Domain.Shared;
using GoodApp.Modules.Notes.Domain;
using GoodApp.Modules.Notes.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace GoodApp.Modules.Notes.Slices.CreateNote;

sealed class CreateNoteHandler(IRepository<Note, NoteId> notes) : RequestHandler<CreateNoteRequest, CreateNoteResponse>
{
    public override async Task<Result<CreateNoteResponse>> HandleAsync(Envelope<CreateNoteRequest> envelope)
    {
        var invoice = envelope.Message.InvoiceId is { } id ? new InvoiceId(id) : (InvoiceId?)null;
        var note = Note.Create(envelope.Message.Text, invoice);
        notes.Add(note);
        await notes.SaveChangesAsync(envelope.Cancellation);
        return new CreateNoteResponse(note.Id.Value);
    }
}
