namespace GoodApp.Notes.Slices.LogInvoice;

using GoodApp.Billing.Slices.CreateInvoice;
using GoodApp.Notes.Domain;

public sealed class InvoiceCreatedHandler : Kit.IEventHandler {
    public static Note Handle(InvoiceCreated created) {
        return new Note(created.Id, "An invoice was created.");
    }
}
