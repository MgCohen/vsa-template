using GoodApp.Billing.Slices.CreateInvoice;
using GoodApp.Notes.Domain;

namespace GoodApp.Notes.Slices.LogInvoice;

public sealed class InvoiceCreatedHandler : Kit.IEventHandler {
    public static Note Handle(InvoiceCreated created) {
        return new Note(created.Id, "An invoice was created.");
    }
}
