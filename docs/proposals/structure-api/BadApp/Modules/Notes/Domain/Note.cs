using BadApp.Modules.Billing.Domain.Shared;
using BadApp.Modules.Notes.Domain.Shared;
using VSA.Domain;

namespace BadApp.Modules.Notes.Domain;

public sealed class Note : AggregateRoot<NoteId>
{
    Note() { }

    public string Text { get; private set; } = "";
    public InvoiceId? Invoice { get; private set; }
    public bool Archived { get; private set; }

    public static Note Create(string text, InvoiceId? invoice) => new() { Id = new(Guid.NewGuid()), Text = text, Invoice = invoice };

    public void Archive() => Archived = true;
}
