namespace BadApp.Notes.Slices.LogInvoice;

using BadApp.Billing.Slices.CreateInvoice;
using BadApp.Notes.Domain;

public sealed class InvoiceCreatedHandler : Kit.IEventHandler {
    public static Note Handle(InvoiceCreated created) {
        return new Note(created.Id, "An invoice was created.");
    }
}
