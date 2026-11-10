using GoodApp.Modules.Billing.Domain.Shared;
using GoodApp.Modules.Notes.Domain.Shared;
using VSA.Domain;

namespace GoodApp.Modules.Notes.Domain;

public sealed class Note : AggregateRoot<NoteId>
{
    Note() { }

    public string Text { get; private set; } = "";
    public InvoiceId? Invoice { get; private set; }
    public bool Archived { get; private set; }

    public static Note Create(string text, InvoiceId? invoice) => new() { Id = new(Guid.NewGuid()), Text = text, Invoice = invoice };

    public void Archive() => Archived = true;
}
