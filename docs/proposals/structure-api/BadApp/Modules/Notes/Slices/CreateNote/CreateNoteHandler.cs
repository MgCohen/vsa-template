using BadApp.Modules.Billing.Domain;
using BadApp.Modules.Billing.Domain.Shared;
using BadApp.Modules.Notes.Domain;
using BadApp.Modules.Notes.Domain.Shared;
using BadApp.Modules.Notes.Slices.ArchiveNote;
using VSA.Contracts;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace BadApp.Modules.Notes.Slices.CreateNote;

sealed class CreateNoteHandler(IRepository<Note, NoteId> notes, IRepository<Invoice, InvoiceId> invoices)
    : RequestHandler<CreateNoteRequest, CreateNoteResponse>
{
    public override async Task<Result<CreateNoteResponse>> HandleAsync(Envelope<CreateNoteRequest> envelope)
    {
        var invoice = envelope.Message.InvoiceId is { } id ? await invoices.GetAsync(new InvoiceId(id), envelope.Cancellation) : null;
        var note = Note.Create(envelope.Message.Text, invoice?.Id);
        notes.Add(note);
        await notes.SaveChangesAsync(envelope.Cancellation);
        _ = new ArchiveNoteRequest(note.Id.Value);
        return new CreateNoteResponse(note.Id.Value);
    }
}
