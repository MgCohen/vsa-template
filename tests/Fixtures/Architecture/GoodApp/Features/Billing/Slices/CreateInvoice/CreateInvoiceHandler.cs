using GoodApp.Billing.Domain;

namespace GoodApp.Billing.Slices.CreateInvoice;

public sealed class CreateInvoiceHandler : Kit.IRequestHandler {
    public static (Invoice, InvoiceCreated) Handle(CreateInvoiceRequest request) {
        var invoice = new Invoice(Guid.NewGuid(), request.Amount);
        return (invoice, new InvoiceCreated(invoice.Id));
    }
}
